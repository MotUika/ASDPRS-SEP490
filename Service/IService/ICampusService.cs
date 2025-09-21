using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Campus;
using Service.RequestAndResponse.Response.Campus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICampusService
    {
        Task<BaseResponse<CampusResponse>> GetCampusByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CampusResponse>>> GetAllCampusesAsync();
        Task<BaseResponse<CampusResponse>> CreateCampusAsync(CreateCampusRequest request);
        Task<BaseResponse<CampusResponse>> UpdateCampusAsync(UpdateCampusRequest request);
        Task<BaseResponse<bool>> DeleteCampusAsync(int id);
    }
}