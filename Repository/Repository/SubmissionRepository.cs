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
    public class SubmissionRepository : BaseRepository<Submission>, ISubmissionRepository
    {
        private readonly ASDPRSContext _context;

        public SubmissionRepository(BaseDAO<Submission> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetByUserIdAsync(int userId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task<Submission> GetSubmissionWithReviewsAsync(int submissionId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Reviews)
                        .ThenInclude(r => r.CriteriaFeedbacks)
                            .ThenInclude(cf => cf.Criteria)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
        }

        public async Task<Submission> GetByAssignmentAndUserAsync(int assignmentId, int userId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);
        }

        public async Task<IEnumerable<Submission>> GetByCourseInstanceAndUserAsync(int courseInstanceId, int userId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                .Where(s => s.Assignment.CourseInstanceId == courseInstanceId && s.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetByUserAndSemesterAsync(int userId, int semesterId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.CourseInstance)
                .Include(s => s.User)
                .Include(s => s.ReviewAssignments)
                .Where(s => s.UserId == userId && s.Assignment.CourseInstance.SemesterId == semesterId)
                .ToListAsync();
        }
    }
}
