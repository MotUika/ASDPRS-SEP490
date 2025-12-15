using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Rubric;
using Service.RequestAndResponse.Response.Rubric;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IRubricService
    {
        Task<BaseResponse<RubricResponse>> GetRubricByIdAsync(int id);
        Task<BaseResponse<IEnumerable<RubricResponse>>> GetAllRubricsAsync();
        Task<BaseResponse<RubricResponse>> CreateRubricAsync(CreateRubricRequest request);
        Task<BaseResponse<RubricResponse>> UpdateRubricAsync(UpdateRubricRequest request);
        Task<BaseResponse<bool>> DeleteRubricAsync(int id);
        Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByTemplateIdAsync(int templateId);
        Task<BaseResponse<IEnumerable<RubricResponse>>> GetRubricsByAssignmentIdAsync(int assignmentId);
        Task<BaseResponse<RubricResponse>> GetRubricWithCriteriaAsync(int rubricId);
        Task<BaseResponse<IEnumerable<RubricResponse>>> GetModifiedRubricsAsync();
        Task<BaseResponse<RubricResponse>> CreateRubricFromTemplateAsync(int templateId, int? assignmentId = null);
        //Task<BaseResponse<IEnumerable<RubricResponse>>> GetPublicRubricsAsync();
        Task<BaseResponse<IEnumerable<RubricResponse>>>GetRubricsByUserIdAsync(int userId, int? courseInstanceId);


    }
}