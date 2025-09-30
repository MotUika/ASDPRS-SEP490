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
    public class CourseStudentRepository : BaseRepository<CourseStudent>, ICourseStudentRepository
    {
        private readonly ASDPRSContext _context;

        public CourseStudentRepository(BaseDAO<CourseStudent> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseStudent>> GetByCourseInstanceIdAsync(int courseInstanceId)
        {
            return await _context.CourseStudents
                .Include(cs => cs.User)
                .Include(cs => cs.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(cs => cs.CourseInstanceId == courseInstanceId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseStudent>> GetByUserIdAsync(int userId)
        {
            return await _context.CourseStudents
                .Include(cs => cs.User)
                .Include(cs => cs.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(cs => cs.UserId == userId)
                .ToListAsync();
        }
        public async Task<CourseStudent> GetByCourseInstanceAndUserAsync(int courseInstanceId, int userId)
        {
            return await _context.CourseStudents
                .FirstOrDefaultAsync(cs => cs.CourseInstanceId == courseInstanceId && cs.UserId == userId);
        }
    }
}
