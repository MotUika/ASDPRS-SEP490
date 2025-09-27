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
    }
}