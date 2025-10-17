using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.CourseStudent;
using Swashbuckle.AspNetCore.Annotations;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý đăng ký lớp học của sinh viên")]
    public class EnrollmentController : ControllerBase
    {
        private readonly ICourseStudentService _courseStudentService;
        private readonly ICourseInstanceService _courseInstanceService;

        public EnrollmentController(ICourseStudentService courseStudentService, ICourseInstanceService courseInstanceService)
        {
            _courseStudentService = courseStudentService;
            _courseInstanceService = courseInstanceService;
        }

        [HttpGet("check/{courseInstanceId}")]
        [SwaggerOperation(
            Summary = "Kiểm tra trạng thái enrollment của sinh viên",
            Description = "Kiểm tra xem sinh viên đã enrolled vào lớp học chưa"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(404, "Sinh viên chưa được import vào lớp")]
        public async Task<IActionResult> CheckEnrollmentStatus(int courseInstanceId, [FromQuery] int studentId)
        {
            var result = await _courseStudentService.GetEnrollmentStatusAsync(courseInstanceId, studentId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("enroll")]
        [SwaggerOperation(
            Summary = "Sinh viên đăng ký lớp học",
            Description = "Sinh viên sử dụng enrollment key để đăng ký vào lớp học"
        )]
        [SwaggerResponse(200, "Đăng ký thành công", typeof(BaseResponse<CourseStudentResponse>))]
        [SwaggerResponse(400, "Key không đúng hoặc đã hết hạn")]
        [SwaggerResponse(404, "Không tìm thấy sinh viên trong lớp")]
        public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentRequest request)
        {
            var result = await _courseStudentService.EnrollStudentAsync(request.CourseInstanceId, request.StudentUserId, request.EnrollKey);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("course/{courseInstanceId}/info")]
        [SwaggerOperation(
            Summary = "Lấy thông tin lớp học để enroll",
            Description = "Lấy thông tin cơ bản của lớp học (không cần enrollment)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<object>))]
        [SwaggerResponse(404, "Không tìm thấy lớp học")]
        public async Task<IActionResult> GetCourseInfoForEnrollment(int courseInstanceId)
        {
            var courseInstance = await _courseInstanceService.GetCourseInstanceByIdAsync(courseInstanceId);
            if (!courseInstance.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)courseInstance.StatusCode, courseInstance);
            }

            // Chỉ trả về thông tin cơ bản, không bao gồm enrollment password
            var response = new
            {
                courseInstance.Data.CourseInstanceId,
                courseInstance.Data.CourseName,
                courseInstance.Data.CourseCode,
                courseInstance.Data.SectionCode,
                courseInstance.Data.CampusName,
                courseInstance.Data.SemesterName,
                RequiresApproval = courseInstance.Data.RequiresApproval,
            };

            return Ok(new BaseResponse<object>(
                "Course information retrieved",
                StatusCodeEnum.OK_200,
                response
            ));
        }

        [HttpGet("student/{studentId}/pending-courses")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lớp học đang chờ enroll",
            Description = "Lấy danh sách các lớp học mà sinh viên đã được import nhưng chưa enroll"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<CourseStudentResponse>>))]
        public async Task<IActionResult> GetPendingEnrollments(int studentId)
        {
            var courseStudents = await _courseStudentService.GetCourseStudentsByStudentAsync(studentId);
            if (!courseStudents.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)courseStudents.StatusCode, courseStudents);
            }

            var pendingCourses = courseStudents.Data
                .Where(cs => cs.Status == "Pending")
                .ToList();

            return Ok(new BaseResponse<List<CourseStudentResponse>>(
                "Pending enrollments retrieved",
                StatusCodeEnum.OK_200,
                pendingCourses
            ));
        }
    }
}

public class EnrollStudentRequest
{
    public int CourseInstanceId { get; set; }
    public int StudentUserId { get; set; }
    public string EnrollKey { get; set; }
}