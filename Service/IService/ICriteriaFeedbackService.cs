using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CriteriaFeedback;
using Service.RequestAndResponse.Response.CriteriaFeedback;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICriteriaFeedbackService
    {
        Task<BaseResponse<CriteriaFeedbackResponse>> GetCriteriaFeedbackByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetAllCriteriaFeedbacksAsync();
        Task<BaseResponse<CriteriaFeedbackResponse>> CreateCriteriaFeedbackAsync(CreateCriteriaFeedbackRequest request);
        Task<BaseResponse<CriteriaFeedbackResponse>> UpdateCriteriaFeedbackAsync(UpdateCriteriaFeedbackRequest request);
        Task<BaseResponse<bool>> DeleteCriteriaFeedbackAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetCriteriaFeedbacksByReviewIdAsync(int reviewId);
        Task<BaseResponse<IEnumerable<CriteriaFeedbackResponse>>> GetCriteriaFeedbacksByCriteriaIdAsync(int criteriaId);
    }
}