using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CourseInstance;
using Service.RequestAndResponse.Response.CourseInstance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICourseInstanceService
    {
        Task<BaseResponse<CourseInstanceResponse>> GetCourseInstanceByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetAllCourseInstancesAsync();
        Task<BaseResponse<CourseInstanceResponse>> CreateCourseInstanceAsync(CreateCourseInstanceRequest request);
        Task<BaseResponse<CourseInstanceResponse>> UpdateCourseInstanceAsync(UpdateCourseInstanceRequest request);
        Task<BaseResponse<bool>> DeleteCourseInstanceAsync(int id);
        Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesByCourseIdAsync(int courseId);
        Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesBySemesterIdAsync(int semesterId);
        Task<BaseResponse<IEnumerable<CourseInstanceResponse>>> GetCourseInstancesByCampusIdAsync(int campusId);
        Task<BaseResponse<string>> UpdateEnrollKeyAsync(int courseInstanceId, string newKey, int userId);
    }
}