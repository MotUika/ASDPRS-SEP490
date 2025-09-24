using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CourseInstructor;
using Service.RequestAndResponse.Response.CourseInstructor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICourseInstructorService
    {
        Task<BaseResponse<CourseInstructorResponse>> CreateCourseInstructorAsync(CreateCourseInstructorRequest request);
        Task<BaseResponse<bool>> DeleteCourseInstructorAsync(int courseInstructorId);
        Task<BaseResponse<CourseInstructorResponse>> GetCourseInstructorByIdAsync(int id);
        Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByCourseInstanceAsync(int courseInstanceId);
        Task<BaseResponse<List<CourseInstructorResponse>>> GetCourseInstructorsByInstructorAsync(int instructorId);
        Task<BaseResponse<bool>> UpdateMainInstructorAsync(int courseInstanceId, int mainInstructorId);
        Task<BaseResponse<List<CourseInstructorResponse>>> BulkAssignInstructorsAsync(BulkAssignInstructorsRequest request);
    }
}