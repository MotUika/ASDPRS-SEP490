using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface INotificationRepository
    {
        Task<Notification> GetByIdAsync(int id);
        Task<Notification> AddAsync(Notification entity);
        Task<Notification> UpdateAsync(Notification entity);
        Task<Notification> DeleteAsync(Notification entity);
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId);
        Task<IEnumerable<Notification>> GetBySenderIdAsync(int senderId);
    }
}
