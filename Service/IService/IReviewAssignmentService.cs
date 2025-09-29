using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.ReviewAssignment;
using Service.RequestAndResponse.Response.ReviewAssignment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IReviewAssignmentService
    {
        Task<BaseResponse<ReviewAssignmentResponse>> CreateReviewAssignmentAsync(CreateReviewAssignmentRequest request);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> BulkCreateReviewAssignmentsAsync(BulkCreateReviewAssignmentRequest request);
        Task<BaseResponse<ReviewAssignmentResponse>> UpdateReviewAssignmentAsync(UpdateReviewAssignmentRequest request);
        Task<BaseResponse<bool>> DeleteReviewAssignmentAsync(int reviewAssignmentId);
        Task<BaseResponse<ReviewAssignmentResponse>> GetReviewAssignmentByIdAsync(int reviewAssignmentId);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsBySubmissionIdAsync(int submissionId);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsByReviewerIdAsync(int reviewerId);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetReviewAssignmentsByAssignmentIdAsync(int assignmentId);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetOverdueReviewAssignmentsAsync();
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewAssignmentsAsync(int reviewerId);
        Task<BaseResponse<bool>> AssignPeerReviewsAutomaticallyAsync(int assignmentId, int reviewsPerSubmission);
        Task<BaseResponse<PeerReviewStatsResponse>> GetPeerReviewStatisticsAsync(int assignmentId);
        Task<BaseResponse<List<ReviewAssignmentResponse>>> GetPendingReviewsForStudentAsync(int studentId, int? courseInstanceId = null);
        Task<BaseResponse<ReviewAssignmentDetailResponse>> GetReviewAssignmentDetailsAsync(int reviewAssignmentId);
    }
}