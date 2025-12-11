using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Course;
using Service.RequestAndResponse.Response.Course;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICourseService
    {
        Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(int id);
        Task<BaseResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync();
        Task<BaseResponse<CourseResponse>> CreateCourseAsync(CreateCourseRequest request);
        Task<BaseResponse<CourseResponse>> UpdateCourseAsync(UpdateCourseRequest request);
        Task<BaseResponse<bool>> DeleteCourseAsync(int id);
        Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByCodeAsync(string courseCode);
        Task<BaseResponse<IEnumerable<CourseResponse>>> GetActiveCoursesAsync();
        Task<BaseResponse<IEnumerable<CourseResponse>>> GetCoursesByUserIdAsync(int userId);
    }
}