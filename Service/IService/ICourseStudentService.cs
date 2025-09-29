using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.CourseInstance;
using Service.RequestAndResponse.Response.CourseStudent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface ICourseStudentService
    {
        Task<BaseResponse<CourseStudentResponse>> CreateCourseStudentAsync(CreateCourseStudentRequest request);
        Task<BaseResponse<bool>> DeleteCourseStudentAsync(int courseStudentId);
        Task<BaseResponse<CourseStudentResponse>> GetCourseStudentByIdAsync(int id);
        Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByCourseInstanceAsync(int courseInstanceId);
        Task<BaseResponse<List<CourseStudentResponse>>> GetCourseStudentsByStudentAsync(int studentId);
        Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentStatusAsync(int courseStudentId, string status, int changedByUserId);
        Task<BaseResponse<CourseStudentResponse>> UpdateCourseStudentGradeAsync(int courseStudentId, decimal? finalGrade, bool isPassed, int changedByUserId);
        Task<BaseResponse<List<CourseStudentResponse>>> BulkAssignStudentsAsync(BulkAssignStudentsRequest request);
        Task<BaseResponse<List<CourseStudentResponse>>> GetStudentsByCourseAndCampusAsync(int courseId, int semesterId, int campusId);
        Task<BaseResponse<CourseStudentResponse>> EnrollStudentAsync(int courseInstanceId, int studentUserId, string enrollKey);
        Task<BaseResponse<List<CourseStudentResponse>>> ImportStudentsFromExcelAsync(int courseInstanceId, Stream excelStream, int? changedByUserId);
        Task<BaseResponse<List<CourseInstanceResponse>>> GetStudentCoursesAsync(int studentId);
    }
}