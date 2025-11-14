using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.IService;
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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                        // Kiểm tra assignment sắp đến hạn (24h tới)
                        await CheckUpcomingDeadlines(assignmentService, notificationService);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in deadline reminder background service");
                }

                // Chạy mỗi giờ
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckUpcomingDeadlines(IAssignmentService assignmentService, INotificationService notificationService)
        {
            try
            {
                var assignments = await assignmentService.GetActiveAssignmentsAsync();
                var now = DateTime.UtcNow;

                foreach (var assignment in assignments.Data)
                {
                    var timeUntilDeadline = assignment.Deadline - now;

                    // Gửi reminder nếu còn 24h hoặc 1h
                    if (timeUntilDeadline <= TimeSpan.FromHours(24) && timeUntilDeadline > TimeSpan.Zero)
                    {
                        await notificationService.SendDeadlineReminderAsync(assignment.AssignmentId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upcoming deadlines");
            }
        }

    }
}
