using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.AISummary;
using Service.RequestAndResponse.Response.AISummary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IAISummaryService
    {
        Task<BaseResponse<AISummaryResponse>> CreateAISummaryAsync(CreateAISummaryRequest request);
        Task<BaseResponse<AISummaryResponse>> UpdateAISummaryAsync(UpdateAISummaryRequest request);
        Task<BaseResponse<bool>> DeleteAISummaryAsync(int summaryId);
        Task<BaseResponse<AISummaryResponse>> GetAISummaryByIdAsync(int summaryId);
        Task<BaseResponse<List<AISummaryResponse>>> GetAISummariesBySubmissionAsync(int submissionId);
        Task<BaseResponse<List<AISummaryResponse>>> GetAISummariesByTypeAsync(string summaryType);
        Task<BaseResponse<AISummaryResponse>> GetAISummaryBySubmissionAndTypeAsync(int submissionId, string summaryType);
        Task<BaseResponse<AISummaryGenerationResponse>> GenerateAISummaryAsync(GenerateAISummaryRequest request);
        Task<BaseResponse<List<AISummaryResponse>>> GetRecentAISummariesAsync(int maxResults = 10);
        Task<BaseResponse<bool>> GenerateAllSummaryTypesAsync(int submissionId, bool forceRegenerate = false);
    }
}