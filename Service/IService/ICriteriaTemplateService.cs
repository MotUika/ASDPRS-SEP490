using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CriteriaTemplate;
using Service.RequestAndResponse.Response.CriteriaTemplate;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICriteriaTemplateService
    {
        Task<BaseResponse<CriteriaTemplateResponse>> GetCriteriaTemplateByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaTemplateResponse>>> GetAllCriteriaTemplatesAsync();
        Task<BaseResponse<CriteriaTemplateResponse>> CreateCriteriaTemplateAsync(CreateCriteriaTemplateRequest request);
        Task<BaseResponse<CriteriaTemplateResponse>> UpdateCriteriaTemplateAsync(UpdateCriteriaTemplateRequest request);
        Task<BaseResponse<bool>> DeleteCriteriaTemplateAsync(int id);
        Task<BaseResponse<IEnumerable<CriteriaTemplateResponse>>> GetCriteriaTemplatesByTemplateIdAsync(int templateId);
    }
}