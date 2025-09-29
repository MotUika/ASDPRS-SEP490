using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Request.Review;
using Service.RequestAndResponse.Response.Review;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IReviewService
    {
        Task<BaseResponse<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request);
        Task<BaseResponse<ReviewResponse>> UpdateReviewAsync(UpdateReviewRequest request);
        Task<BaseResponse<bool>> DeleteReviewAsync(int reviewId);
        Task<BaseResponse<ReviewResponse>> GetReviewByIdAsync(int reviewId);
        Task<BaseResponse<List<ReviewResponse>>> GetReviewsByReviewAssignmentIdAsync(int reviewAssignmentId);
        Task<BaseResponse<List<ReviewResponse>>> GetReviewsBySubmissionIdAsync(int submissionId);
        Task<BaseResponse<List<ReviewResponse>>> GetReviewsByReviewerIdAsync(int reviewerId);
        Task<BaseResponse<List<ReviewResponse>>> GetReviewsByAssignmentIdAsync(int assignmentId);
        Task<BaseResponse<ReviewResponse>> CreateAIReviewAsync(CreateReviewRequest request);
        Task<BaseResponse<ReviewResponse>> SubmitStudentReviewAsync(SubmitStudentReviewRequest request);
    }
}