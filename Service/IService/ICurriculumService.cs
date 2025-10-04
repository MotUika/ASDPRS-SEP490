using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Curriculum;
using Service.RequestAndResponse.Response.Curriculum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICurriculumService
    {
        Task<BaseResponse<CurriculumResponse>> GetCurriculumByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetAllCurriculumsAsync();
        Task<BaseResponse<CurriculumResponse>> CreateCurriculumAsync(CreateCurriculumRequest request);
        Task<BaseResponse<CurriculumResponse>> UpdateCurriculumAsync(UpdateCurriculumRequest request);
        Task<BaseResponse<bool>> DeleteCurriculumAsync(int id);
        Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetCurriculumsByCampusAsync(int campusId);
        Task<BaseResponse<IEnumerable<CurriculumResponse>>> GetCurriculumsByMajorAsync(int majorId);
    }
}