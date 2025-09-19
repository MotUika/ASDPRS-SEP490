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
    public class CourseInstructorRepository : BaseRepository<CourseInstructor>, ICourseInstructorRepository
    {
        private readonly ASDPRSContext _context;

        public CourseInstructorRepository(BaseDAO<CourseInstructor> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseInstructor>> GetByCourseInstanceIdAsync(int courseInstanceId)
        {
            return await _context.CourseInstructors
                .Include(ci => ci.User)
                .Include(ci => ci.CourseInstance)
                .ThenInclude(c => c.Course)
                .Where(ci => ci.CourseInstanceId == courseInstanceId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseInstructor>> GetByUserIdAsync(int userId)
        {
            return await _context.CourseInstructors
                .Include(ci => ci.User)
                .Include(ci => ci.CourseInstance)
                .ThenInclude(c => c.Course)
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
        }
    }
}
