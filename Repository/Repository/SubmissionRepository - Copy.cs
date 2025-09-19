using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        private readonly ASDPRSContext _context;

        public NotificationRepository(BaseDAO<Notification> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.SenderUser)
                .Include(n => n.Assignment)
                .Include(n => n.Submission)
                .Include(n => n.ReviewAssignment)
                .Where(n => n.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.SenderUser)
                .Include(n => n.Assignment)
                .Include(n => n.Submission)
                .Include(n => n.ReviewAssignment)
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetBySenderIdAsync(int senderId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.SenderUser)
                .Include(n => n.Assignment)
                .Include(n => n.Submission)
                .Include(n => n.ReviewAssignment)
                .Where(n => n.SenderUserId == senderId)
                .ToListAsync();
        }
    }
}
