using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using DataAccessLayer.DAO;
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
    public class CourseInstanceRepository : BaseRepository<CourseInstance>, ICourseInstanceRepository
    {
        private readonly ASDPRSContext _context;

        public CourseInstanceRepository(CourseInstanceDAO courseInstanceDao, ASDPRSContext context) : base(courseInstanceDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<CourseInstance>> GetByCourseIdAsync(int courseId)
        {
            return await _context.CourseInstances
                .Include(ci => ci.Course)
                .Include(ci => ci.Semester)
                .Include(ci => ci.Campus)
                .Where(ci => ci.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseInstance>> GetBySemesterIdAsync(int semesterId)
        {
            return await _context.CourseInstances
                .Include(ci => ci.Course)
                .Include(ci => ci.Semester)
                .Include(ci => ci.Campus)
                .Where(ci => ci.SemesterId == semesterId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseInstance>> GetByCampusIdAsync(int campusId)
        {
            return await _context.CourseInstances
                .Include(ci => ci.Course)
                .Include(ci => ci.Semester)
                .Include(ci => ci.Campus)
                .Where(ci => ci.CampusId == campusId)
                .ToListAsync();
        }
    }
}
