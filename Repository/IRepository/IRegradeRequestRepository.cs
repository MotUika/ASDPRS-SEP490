using BussinessObject.Models;
using Repository.IBaseRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.IRepository
{
    public interface IRegradeRequestRepository : IBaseRepository<RegradeRequest>
    {
        // Các phương thức đặc thù cho RegradeRequest
        Task<IEnumerable<RegradeRequest>> GetBySubmissionIdAsync(int submissionId);
        Task<IEnumerable<RegradeRequest>> GetByStatusAsync(string status);
        Task<IEnumerable<RegradeRequest>> GetByStudentIdAsync(int studentId);
        Task<IEnumerable<RegradeRequest>> GetByInstructorIdAsync(int instructorId);
        Task<IEnumerable<RegradeRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<RegradeRequest>> GetRequestsByAssignmentIdAsync(int assignmentId);
        Task<RegradeRequest> UpdateRequestStatusAsync(int requestId, string status, string resolutionNotes, int? reviewedByInstructorId);
        Task<bool> HasPendingRequestForSubmissionAsync(int submissionId);
    }
}