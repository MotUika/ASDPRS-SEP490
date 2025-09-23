using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class AssignmentRepository : BaseRepository<Assignment>, IAssignmentRepository
    {
        private readonly ASDPRSContext _context;

        public AssignmentRepository(BaseDAO<Assignment> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Assignment>> GetByCourseInstanceIdAsync(int courseInstanceId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Campus)
                .Include(a => a.Rubric)
                .Where(a => a.CourseInstanceId == courseInstanceId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Rubric)
                .ThenInclude(r => r.Criteria)
                .ThenInclude(c => c.CriteriaTemplate)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Campus)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByInstructorAsync(int instructorId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Campus)
                .Where(a => a.CourseInstance.CourseInstructors.Any(ci => ci.UserId == instructorId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(int studentId)
        {
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Campus)
                .Where(a => a.CourseInstance.CourseStudents.Any(cs => cs.UserId == studentId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetActiveAssignmentsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(a => (a.StartDate == null || a.StartDate <= now) && a.Deadline >= now)
                .OrderBy(a => a.Deadline)
                .ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Assignments
                .Include(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(a => a.Deadline < now)
                .OrderByDescending(a => a.Deadline)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int assignmentId)
        {
            return await _context.Assignments.AnyAsync(a => a.AssignmentId == assignmentId);
        }

        Task IAssignmentRepository.AddAsync(Assignment assignment)
        {
            return AddAsync(assignment);
        }

        Task IAssignmentRepository.UpdateAsync(Assignment assignment)
        {
            return UpdateAsync(assignment);
        }

        Task IAssignmentRepository.DeleteAsync(Assignment assignment)
        {
            return DeleteAsync(assignment);
        }
    }
}