using BussinessObject.Models;
using Repository.IBaseRepository;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
}