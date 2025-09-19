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
                .Include(a => a.Rubric)
                .Where(a => a.CourseInstanceId == courseInstanceId)
                .ToListAsync();
        }

        public async Task<Assignment> GetAssignmentWithRubricAsync(int assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Rubric)
                .ThenInclude(r => r.Criteria)
                .ThenInclude(c => c.CriteriaTemplate)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
        }
    }
}
