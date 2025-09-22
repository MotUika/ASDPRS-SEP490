using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.RubricTemplate;
using Service.RequestAndResponse.Response.RubricTemplate;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IRubricTemplateService
    {
        Task<BaseResponse<RubricTemplateResponse>> GetRubricTemplateByIdAsync(int id);
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetAllRubricTemplatesAsync();
        Task<BaseResponse<RubricTemplateResponse>> CreateRubricTemplateAsync(CreateRubricTemplateRequest request);
        Task<BaseResponse<RubricTemplateResponse>> UpdateRubricTemplateAsync(UpdateRubricTemplateRequest request);
        Task<BaseResponse<bool>> DeleteRubricTemplateAsync(int id);
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserIdAsync(int userId);
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetPublicRubricTemplatesAsync();
    }
}