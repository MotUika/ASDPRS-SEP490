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
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> SearchRubricTemplatesAsync(string searchTerm);
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetRubricTemplatesByUserAndCourseAsync(int userId, int courseId);
        Task<BaseResponse<IEnumerable<RubricTemplateResponse>>> GetPublicRubricTemplatesByUserIdAsync(int userId);
        Task<BaseResponse<RubricTemplateResponse>> ToggleRubricTemplatePublicStatusAsync(int templateId, bool makePublic);
    }
}