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
    public class ReviewAssignmentRepository : BaseRepository<ReviewAssignment>, IReviewAssignmentRepository
    {
        private readonly ASDPRSContext _context;

        public ReviewAssignmentRepository(BaseDAO<ReviewAssignment> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReviewAssignment>> GetBySubmissionIdAsync(int submissionId)
        {
            return await _context.ReviewAssignments
                .Include(ra => ra.ReviewerUser)
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.User) // Sửa StudentUser thành User
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(ra => ra.SubmissionId == submissionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReviewAssignment>> GetByReviewerIdAsync(int reviewerId)
        {
            return await _context.ReviewAssignments
                .Include(ra => ra.ReviewerUser)
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.User) // Sửa StudentUser thành User
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(ra => ra.ReviewerUserId == reviewerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReviewAssignment>> GetByAssignmentIdAsync(int assignmentId)
        {
            return await _context.ReviewAssignments
                .Include(ra => ra.ReviewerUser)
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.User) // Sửa StudentUser thành User
                .Include(ra => ra.Submission)
                .ThenInclude(s => s.Assignment)
                .ThenInclude(a => a.CourseInstance)
                .ThenInclude(ci => ci.Course)
                .Where(ra => ra.Submission.AssignmentId == assignmentId)
                .ToListAsync();
        }

        Task IReviewAssignmentRepository.AddAsync(ReviewAssignment reviewAssignment)
        {
             return AddAsync(reviewAssignment);
        }
         
         Task IReviewAssignmentRepository.UpdateAsync(ReviewAssignment reviewAssignment)
        {
             return UpdateAsync(reviewAssignment);
        }
        
        Task IReviewAssignmentRepository.DeleteAsync(ReviewAssignment reviewAssignment)
        {
             return DeleteAsync(reviewAssignment);
        }
    }
}