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
            // Cập nhật mỗi 1 giờ 1 lần (có thể chỉnh)
            _timer = new Timer(UpdateStatuses, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private async void UpdateStatuses(object state)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ASDPRSContext>();
            var assignments = await context.Assignments.ToListAsync();

            foreach (var a in assignments)
            {
                var newStatus = CalculateAssignmentStatus(a);
                if (a.Status != newStatus)
                {
                    a.Status = newStatus;
                }
            }

            await context.SaveChangesAsync();
        }

        private string CalculateAssignmentStatus(Assignment assignment)
        {
            var now = DateTime.UtcNow;

            if (assignment.StartDate.HasValue && now < assignment.StartDate.Value)
                return AssignmentStatusEnum.Scheduled.ToString();

            if (now >= assignment.StartDate && now <= assignment.Deadline)
                return AssignmentStatusEnum.Active.ToString();

            if (assignment.ReviewDeadline.HasValue && now <= assignment.ReviewDeadline.Value)
                return AssignmentStatusEnum.InReview.ToString();

            if (assignment.FinalDeadline.HasValue && now <= assignment.FinalDeadline.Value)
                return AssignmentStatusEnum.Closed.ToString();

            return AssignmentStatusEnum.Archived.ToString();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
