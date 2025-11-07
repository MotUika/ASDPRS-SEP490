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
                .Include(u => u.Major)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetByCampusIdAsync(int campusId)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.Major)
                .Where(u => u.CampusId == campusId)
                .ToListAsync();
        }

        
        public async Task<User> GetByStudentCodeAsync(string studentCode)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.Major) 
                .FirstOrDefaultAsync(u => u.StudentCode == studentCode);
        }

        public async Task<IEnumerable<User>> GetByMajorIdAsync(int majorId)
        {
            return await _context.Users
                .Include(u => u.Campus)
                .Include(u => u.Major)
                .Where(u => u.MajorId == majorId)
                .ToListAsync();
        }
    }
}