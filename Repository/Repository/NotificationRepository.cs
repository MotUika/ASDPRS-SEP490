using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;

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
                .Where(n => n.UserId == userId)
                .ToListAsync();
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllReadByUserIdAsync(int userId)
        {
            var readNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();

            if (!readNotifications.Any()) return false;

            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllByUserIdAsync(int userId)
        {
            var allNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            if (!allNotifications.Any()) return false;

            _context.Notifications.RemoveRange(allNotifications);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}