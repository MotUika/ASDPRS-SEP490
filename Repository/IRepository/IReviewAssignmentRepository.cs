using BussinessObject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IReviewAssignmentRepository
    {
        Task<ReviewAssignment> GetByIdAsync(int id);
        Task<IEnumerable<ReviewAssignment>> GetBySubmissionIdAsync(int submissionId);
        Task<IEnumerable<ReviewAssignment>> GetByReviewerIdAsync(int reviewerId);
        Task AddAsync(ReviewAssignment reviewAssignment);
        Task UpdateAsync(ReviewAssignment reviewAssignment);
        Task DeleteAsync(ReviewAssignment reviewAssignment);
        Task<IEnumerable<ReviewAssignment>> GetOverdueAsync(DateTime currentTime);
        Task<List<Submission>> GetAvailableSubmissionsForReviewerAsync(int assignmentId, int reviewerId);
        Task<decimal?> GetPeerAverageScoreBySubmissionIdAsync(int submissionId);
    }
}