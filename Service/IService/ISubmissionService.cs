using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.Submission;
using Service.RequestAndResponse.BaseResponse;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ISubmissionService
    {
        Task<BaseResponse<SubmissionResponse>> CreateSubmissionAsync(CreateSubmissionRequest request);
        Task<BaseResponse<SubmissionResponse>> SubmitAssignmentAsync(SubmitAssignmentRequest request);
        Task<BaseResponse<SubmissionResponse>> GetSubmissionByIdAsync(GetSubmissionByIdRequest request);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByFilterAsync(GetSubmissionsByFilterRequest request);
        Task<BaseResponse<SubmissionResponse>> UpdateSubmissionAsync(UpdateSubmissionRequest request);
        Task<BaseResponse<SubmissionResponse>> UpdateSubmissionStatusAsync(UpdateSubmissionStatusRequest request);
        Task<BaseResponse<bool>> DeleteSubmissionAsync(int submissionId);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByAssignmentIdAsync(int assignmentId, int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<SubmissionListResponse>> GetSubmissionsByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<SubmissionStatisticsResponse>> GetSubmissionStatisticsAsync(int assignmentId);
        Task<BaseResponse<bool>> CheckSubmissionExistsAsync(int assignmentId, int userId);
        Task<BaseResponse<SubmissionResponse>> GetSubmissionWithDetailsAsync(int submissionId);
    }
}