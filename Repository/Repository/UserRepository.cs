using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.DAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;

namespace Repository.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly ASDPRSContext _context;

        public UserRepository(UserDAO userDao, ASDPRSContext context) : base(userDao)
        {
            _context = context;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetByCampusIdAsync(int campusId)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.CampusId == campusId)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == roleName))
                .ToListAsync();
        }

        public async Task<User> GetUserWithRolesAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}