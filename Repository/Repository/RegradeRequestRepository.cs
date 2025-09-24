using BussinessObject.Models;
using DataAccessLayer.DAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class RegradeRequestRepository : BaseRepository<RegradeRequest>, IRegradeRequestRepository
    {
        private readonly RegradeRequestDAO _regradeRequestDAO;

        public RegradeRequestRepository(RegradeRequestDAO regradeRequestDAO) : base(regradeRequestDAO)
        {
            _regradeRequestDAO = regradeRequestDAO;
        }

        public async Task<IEnumerable<RegradeRequest>> GetBySubmissionIdAsync(int submissionId)
        {
            return await _regradeRequestDAO.GetAll()
                .Where(r => r.SubmissionId == submissionId)
                .Include(r => r.Submission)
                .Include(r => r.ReviewedByInstructor)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegradeRequest>> GetByStatusAsync(string status)
        {
            return await _regradeRequestDAO.GetAll()
                .Where(r => r.Status == status)
                .Include(r => r.Submission)
                    .ThenInclude(s => s.Assignment)
                .Include(r => r.Submission.User)
                .Include(r => r.ReviewedByInstructor)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegradeRequest>> GetByStudentIdAsync(int studentId)
        {
            return await _regradeRequestDAO.GetAll()
                .Include(r => r.Submission)
                .Where(r => r.Submission.UserId == studentId)
                .Include(r => r.ReviewedByInstructor)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegradeRequest>> GetByInstructorIdAsync(int instructorId)
        {
            return await _regradeRequestDAO.GetAll()
                .Where(r => r.ReviewedByInstructorId == instructorId)
                .Include(r => r.Submission)
                    .ThenInclude(s => s.Assignment)
                .Include(r => r.Submission.User)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RegradeRequest>> GetPendingRequestsAsync()
        {
            return await GetByStatusAsync("Pending");
        }

        public async Task<IEnumerable<RegradeRequest>> GetRequestsByAssignmentIdAsync(int assignmentId)
        {
            return await _regradeRequestDAO.GetAll()
                .Include(r => r.Submission)
                .Where(r => r.Submission.AssignmentId == assignmentId)
                .Include(r => r.Submission.User)
                .Include(r => r.ReviewedByInstructor)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<RegradeRequest> UpdateRequestStatusAsync(int requestId, string status, string resolutionNotes, int? reviewedByInstructorId)
        {
            var request = await _regradeRequestDAO.GetByIdAsync(requestId);
            if (request == null)
                return null;

            request.Status = status;
            request.ResolutionNotes = resolutionNotes;
            request.ReviewedByInstructorId = reviewedByInstructorId;

            return await _regradeRequestDAO.UpdateAsync(request);
        }

        public async Task<bool> HasPendingRequestForSubmissionAsync(int submissionId)
        {
            return await _regradeRequestDAO.GetAll()
                .AnyAsync(r => r.SubmissionId == submissionId && r.Status == "Pending");
        }
    }
}