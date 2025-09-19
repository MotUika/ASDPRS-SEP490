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
    public class CourseRepository : BaseRepository<Course>, ICourseRepository
    {
        private readonly ASDPRSContext _context;

        public CourseRepository(BaseDAO<Course> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Course>> GetByCurriculumIdAsync(int curriculumId)
        {
            return await _context.Courses
                .Include(c => c.Curriculum)
                .Where(c => c.CurriculumId == curriculumId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetByCourseCodeAsync(string courseCode)
        {
            return await _context.Courses
                .Include(c => c.Curriculum)
                .Where(c => c.CourseCode.Contains(courseCode))
                .ToListAsync();
        }
    }
}
