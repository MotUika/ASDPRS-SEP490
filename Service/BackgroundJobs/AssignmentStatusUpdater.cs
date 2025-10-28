using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.RequestAndResponse.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BussinessObject.Models;

namespace Service.BackgroundJobs
{
    public class AssignmentStatusUpdater : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceScopeFactory _scopeFactory;

        public AssignmentStatusUpdater(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(UpdateStatuses, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async void UpdateStatuses(object state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ASDPRSContext>();

                var assignments = await context.Assignments
                    .Where(a => a.Status != AssignmentStatusEnum.Draft.ToString()) // Chỉ loại Draft
                    .Include(a => a.Submissions)
                    .ToListAsync();

                var updatedCount = 0;

                foreach (var assignment in assignments)
                {
                    var newStatus = CalculateAssignmentStatus(assignment, context);
                    if (assignment.Status != newStatus)
                    {
                        assignment.Status = newStatus;
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Updated {updatedCount} assignment statuses at {DateTime.UtcNow}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating assignment statuses: {ex.Message}");
            }
        }

        private string CalculateAssignmentStatus(Assignment assignment, ASDPRSContext context)
        {
            var now = DateTime.UtcNow;

            // 1. Upcoming - chưa đến StartDate
            if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                return AssignmentStatusEnum.Upcoming.ToString();

            // 2. Active - đang trong thời gian nộp bài
            if (now <= assignment.Deadline)
                return AssignmentStatusEnum.Active.ToString();

            // 3. Kiểm tra nếu không có bài nộp thì Cancelled
            var hasSubmissions = context.Submissions.Any(s => s.AssignmentId == assignment.AssignmentId);
            if (!hasSubmissions)
                return AssignmentStatusEnum.Cancelled.ToString();

            // 4. InReview - cho STUDENT: từ sau Deadline đến ReviewDeadline
            if (now <= assignment.ReviewDeadline)
                return AssignmentStatusEnum.InReview.ToString();

            // 5. Closed - sau ReviewDeadline
            return AssignmentStatusEnum.Closed.ToString();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}