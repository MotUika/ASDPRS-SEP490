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
    public class ReviewRepository : BaseRepository<Review>, IReviewRepository
    {
        private readonly ASDPRSContext _context;

        public ReviewRepository(BaseDAO<Review> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetByReviewAssignmentIdAsync(int reviewAssignmentId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewAssignment)
                .ThenInclude(ra => ra.ReviewerUser)
                .Include(r => r.CriteriaFeedbacks)
                .ThenInclude(cf => cf.Criteria)
                .Where(r => r.ReviewAssignmentId == reviewAssignmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetBySubmissionIdAsync(int submissionId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewAssignment)
                .ThenInclude(ra => ra.ReviewerUser)
                .Include(r => r.CriteriaFeedbacks)
                .ThenInclude(cf => cf.Criteria)
                .Where(r => r.ReviewAssignment.SubmissionId == submissionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByReviewerIdAsync(int reviewerId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewAssignment)
                .ThenInclude(ra => ra.ReviewerUser)
                .Include(r => r.CriteriaFeedbacks)
                .ThenInclude(cf => cf.Criteria)
                .Where(r => r.ReviewAssignment.ReviewerUserId == reviewerId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Review>> GetByAssignmentIdAsync(int assignmentId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewAssignment)
                .ThenInclude(ra => ra.Submission)
                .Where(r => r.ReviewAssignment.Submission.AssignmentId == assignmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetPeerReviewsBySubmissionIdAsync(int submissionId)
        {
            return await _context.ReviewAssignments
                .Where(ra => ra.SubmissionId == submissionId)
                .Include(ra => ra.Reviews)
                .SelectMany(ra => ra.Reviews)
                .Include(r => r.ReviewAssignment)
                    .ThenInclude(ra => ra.ReviewerUser)
                .ToListAsync();
        }

    }
}
