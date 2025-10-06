using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Major;
using Service.RequestAndResponse.Response.Major;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IMajorService
    {
        Task<BaseResponse<MajorResponse>> GetMajorByIdAsync(int id);
        Task<BaseResponse<IEnumerable<MajorResponse>>> GetAllMajorsAsync();
        Task<BaseResponse<MajorResponse>> CreateMajorAsync(CreateMajorRequest request);
        Task<BaseResponse<MajorResponse>> UpdateMajorAsync(UpdateMajorRequest request);
        Task<BaseResponse<bool>> DeleteMajorAsync(int id);
    }
}
