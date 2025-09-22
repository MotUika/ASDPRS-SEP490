using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Criteria;
using Service.RequestAndResponse.Response.Criteria;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICriteriaService
    {
        Task<BaseResponse<CriteriaResponse>> GetCriteriaByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetAllCriteriaAsync();
        Task<BaseResponse<CriteriaResponse>> CreateCriteriaAsync(CreateCriteriaRequest request);
        Task<BaseResponse<CriteriaResponse>> UpdateCriteriaAsync(UpdateCriteriaRequest request);
        Task<BaseResponse<bool>> DeleteCriteriaAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetCriteriaByRubricIdAsync(int rubricId);
        Task<BaseResponse<IEnumerable<CriteriaResponse>>> GetCriteriaByTemplateIdAsync(int criteriaTemplateId);
    }
}