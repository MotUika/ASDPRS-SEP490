using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.AcademicYear;
using Service.RequestAndResponse.Response.AcademicYear;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IAcademicYearService
    {
        Task<BaseResponse<AcademicYearResponse>> GetAcademicYearByIdAsync(int id);
        Task<BaseResponse<IEnumerable<AcademicYearResponse>>> GetAllAcademicYearsAsync();
        Task<BaseResponse<AcademicYearResponse>> CreateAcademicYearAsync(CreateAcademicYearRequest request);
        Task<BaseResponse<AcademicYearResponse>> UpdateAcademicYearAsync(UpdateAcademicYearRequest request);
        Task<BaseResponse<bool>> DeleteAcademicYearAsync(int id);
        Task<BaseResponse<IEnumerable<AcademicYearResponse>>> GetAcademicYearsByCampusAsync(int campusId);
    }
}