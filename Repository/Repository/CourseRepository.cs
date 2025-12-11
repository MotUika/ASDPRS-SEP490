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

        public async Task<IEnumerable<Course>> GetByCourseCodeAsync(string courseCode)
        {
            return await _context.Courses
                .Where(c => c.CourseCode.Contains(courseCode))
                .ToListAsync();
        }
    }
}
