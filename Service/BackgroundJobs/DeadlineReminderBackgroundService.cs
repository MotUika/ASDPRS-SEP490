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
using System.Text;
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
                        var assignmentRepository = scope.ServiceProvider.GetRequiredService<IAssignmentRepository>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var context = scope.ServiceProvider.GetRequiredService<ASDPRSContext>();

                        await CheckUpcomingDeadlines(assignmentRepository, notificationService, context);
                        await CheckNewActiveAssignments(assignmentRepository, notificationService, context);
                    }

                    // Chạy mỗi 30 phút
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Deadline Reminder Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Chờ 5 phút nếu có lỗi
                }
            }
        }

        private async Task CheckUpcomingDeadlines(IAssignmentRepository assignmentRepository, INotificationService notificationService, ASDPRSContext context)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var reminderTime = now.AddHours(24); // Nhắc 24h trước deadline

            var assignmentsDueSoon = await context.Assignments
                .Where(a => a.Deadline <= reminderTime &&
                           a.Deadline > now &&
                           a.Status == "Active")
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.CourseStudents)
                .ToListAsync();

            foreach (var assignment in assignmentsDueSoon)
            {
                foreach (var student in assignment.CourseInstance.CourseStudents)
                {
                    var notificationRequest = new CreateNotificationRequest
                    {
                        UserId = student.UserId,
                        Title = "Assignment Deadline Reminder",
                        Message = $"Assignment '{assignment.Title}' is due on {assignment.Deadline:dd/MM/yyyy HH:mm}. Please submit your work before the deadline.",
                        Type = "DeadlineReminder",
                        AssignmentId = assignment.AssignmentId
                    };

                    await notificationService.CreateNotificationAsync(notificationRequest);
                }

                _logger.LogInformation($"Sent deadline reminders for assignment {assignment.AssignmentId} to {assignment.CourseInstance.CourseStudents.Count} students");
            }
        }

        private async Task CheckNewActiveAssignments(IAssignmentRepository assignmentRepository, INotificationService notificationService, ASDPRSContext context)
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

                _logger.LogInformation($"Sent new assignment notifications for assignment {assignment.AssignmentId} to {assignment.CourseInstance.CourseStudents.Count} students");
            }
        }
    }
}
