using BussinessObject.Models;
using Repository.IBaseRepository;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAllReadByUserIdAsync(int userId);
    Task<bool> DeleteAllByUserIdAsync(int userId);
}