using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Service.BackgroundJobs
{
    public class CourseStatusWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CourseStatusWorker> _logger;

        public CourseStatusWorker(IServiceProvider serviceProvider, ILogger<CourseStatusWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CourseStatusWorker started running...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ASDPRSContext>();
                        var now = DateTime.UtcNow.AddHours(7);

                        var expiredCourses = await context.CourseInstances
                            .Where(c => c.IsActive && c.EndDate < now)
                            .ToListAsync(stoppingToken);

                        if (expiredCourses.Any())
                        {
                            foreach (var course in expiredCourses)
                            {
                                course.IsActive = false;
                                _logger.LogInformation($"Auto-deactivating CourseInstanceId: {course.CourseInstanceId} because EndDate ({course.EndDate}) passed.");
                            }

                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Deactivated {expiredCourses.Count} expired courses.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while auto-deactivating courses.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
