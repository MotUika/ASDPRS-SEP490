using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Response.RegradeRequest;
using Service.RequestAndResponse.BaseResponse;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IRegradeRequestService
    {
        Task<BaseResponse<RegradeRequestResponse>> CreateRegradeRequestAsync(CreateRegradeRequestRequest request);
        Task<BaseResponse<RegradeRequestResponse>> GetRegradeRequestByIdAsync(GetRegradeRequestByIdRequest request);
        Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByFilterAsync(GetRegradeRequestsByFilterRequest request);
        Task<BaseResponse<RegradeRequestResponse>> UpdateRegradeRequestAsync(UpdateRegradeRequestRequest request);
        Task<BaseResponse<RegradeRequestResponse>> UpdateRegradeRequestStatusAsync(UpdateRegradeRequestStatusRequest request);
        Task<BaseResponse<bool>> CheckPendingRequestExistsAsync(int submissionId);
        Task<BaseResponse<RegradeRequestListResponse>> GetPendingRegradeRequestsAsync(int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByStudentIdAsync(int studentId, int pageNumber = 1, int pageSize = 20);
        Task<BaseResponse<RegradeRequestListResponse>> GetRegradeRequestsByInstructorIdAsync(int userID);
        Task<BaseResponse<RegradeRequestResponse>> ReviewRegradeRequestAsync(UpdateRegradeRequestStatusByUserRequest request);
        Task<BaseResponse<RegradeRequestResponse>> CompleteRegradeRequestAsync(UpdateRegradeRequestStatusByUserRequest request);

    }
}