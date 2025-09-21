using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Semester;
using Service.RequestAndResponse.Response.Semester;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ISemesterService
    {
        Task<BaseResponse<SemesterResponse>> GetSemesterByIdAsync(int id);
        Task<BaseResponse<IEnumerable<SemesterResponse>>> GetAllSemestersAsync();
        Task<BaseResponse<SemesterResponse>> CreateSemesterAsync(CreateSemesterRequest request);
        Task<BaseResponse<SemesterResponse>> UpdateSemesterAsync(UpdateSemesterRequest request);
        Task<BaseResponse<bool>> DeleteSemesterAsync(int id);
        Task<BaseResponse<IEnumerable<SemesterResponse>>> GetSemestersByAcademicYearAsync(int academicYearId);
    }
}