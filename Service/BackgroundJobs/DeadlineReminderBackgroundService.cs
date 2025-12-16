using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.IRepository;
using Service.IService;
using Service.RequestAndResponse.Request.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Service.BackgroundJobs
{
    public class DeadlineReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DeadlineReminderBackgroundService> _logger;

        public DeadlineReminderBackgroundService(IServiceProvider services, ILogger<DeadlineReminderBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deadline Reminder Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {

                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var context = scope.ServiceProvider.GetRequiredService<ASDPRSContext>();

                        await CheckNewActiveAssignments(notificationService, context);

                        await CheckDeadlineReminder(notificationService, context, hoursBeforeDeadline: 48, notificationType: "DeadlineReminder_48h");

                        await CheckDeadlineReminder(notificationService, context, hoursBeforeDeadline: 24, notificationType: "DeadlineReminder_24h");
                    }

                    // Chạy mỗi 30 phút
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Deadline Reminder Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task CheckDeadlineReminder(INotificationService notificationService, ASDPRSContext context, int hoursBeforeDeadline, string notificationType)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var limitTime = now.AddHours(hoursBeforeDeadline);

            var assignments = await context.Assignments
                .Where(a => a.Deadline <= limitTime && 
                            a.Deadline > now &&        
                            a.Status == "Active")
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                // Chỉ nhắc những sinh viên chưa nộp bài
                foreach (var student in assignment.CourseInstance.CourseStudents)
                {
                    bool hasSubmitted = await context.Submissions.AnyAsync(s =>
                        s.AssignmentId == assignment.AssignmentId &&
                        s.UserId == student.UserId);

                    if (hasSubmitted) continue;

                    bool alreadySent = await context.Notifications.AnyAsync(n =>
                        n.UserId == student.UserId &&
                        n.AssignmentId == assignment.AssignmentId &&
                        n.Type == notificationType);

                    if (alreadySent) continue;

                    // 3. Gửi thông báo
                    var timeText = hoursBeforeDeadline == 24 ? "1 day" : "2 days";
                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = student.UserId,
                        Title = "Assignment Deadline Reminder",
                        Message = $"Reminder: You have less than {timeText} to submit assignment '{assignment.Title}'. Deadline: {assignment.Deadline:dd/MM/yyyy HH:mm}.",
                        Type = notificationType,
                        AssignmentId = assignment.AssignmentId
                    };

                    await notificationService.CreateNotificationAsync(notificationRequest);
                }

            }
        }

        private async Task CheckNewActiveAssignments(INotificationService notificationService, ASDPRSContext context)
        {
            var lastHour = DateTime.UtcNow.AddHours(7).AddHours(-1);

            var newActiveAssignments = await context.Assignments
                .Where(a => a.Status == "Active" &&
                            a.CreatedAt >= lastHour)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
                .ToListAsync();

            foreach (var assignment in newActiveAssignments)
            {
                foreach (var student in assignment.CourseInstance.CourseStudents)
                {
                    bool alreadySent = await context.Notifications.AnyAsync(n =>
                        n.UserId == student.UserId &&
                        n.AssignmentId == assignment.AssignmentId &&
                        n.Type == "AssignmentActive");

                    if (alreadySent) continue;

                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = student.UserId,
                        Title = "New Assignment Available",
                        Message = $"A new assignment '{assignment.Title}' is now available. Deadline: {assignment.Deadline:dd/MM/yyyy HH:mm}",
                        Type = "AssignmentActive",
                        AssignmentId = assignment.AssignmentId
                    };

                    await notificationService.CreateNotificationAsync(notificationRequest);
                }

                _logger.LogInformation($"Processed new assignment notifications for assignment {assignment.AssignmentId}");
            }
        }
    }
}