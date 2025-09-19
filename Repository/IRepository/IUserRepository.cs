using BussinessObject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int id);
        Task<User> AddAsync(User entity);
        Task<User> UpdateAsync(User entity);
        Task<User> DeleteAsync(User entity);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetByCampusIdAsync(int campusId);
        Task<IEnumerable<User>> GetByRoleAsync(string roleName);
        Task<User> GetUserWithRolesAsync(int userId);
    }
}
