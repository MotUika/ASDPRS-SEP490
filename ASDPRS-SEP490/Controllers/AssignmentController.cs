using MathNet.Numerics.Distributions;
using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Assignment;
using Service.RequestAndResponse.Response.Assignment;
using Service.Service;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý bài tập: CRUD, thống kê, gia hạn deadline, quản lý rubric")]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly ICourseStudentService _courseStudentService;

        public AssignmentController(IAssignmentService assignmentService, ICourseStudentService courseStudentService)
        {
            _assignmentService = assignmentService;
            _courseStudentService = courseStudentService;
        }

        // Lấy assignment theo Id
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin bài tập theo ID",
            Description = "Trả về thông tin chi tiết của bài tập dựa trên ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AssignmentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài tập")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAssignmentById(int id)
        {
            return await CheckEnrollmentByAssignmentAndExecute(id, async () =>
            {
                var result = await _assignmentService.GetAssignmentByIdAsync(id);
                return result.StatusCode switch
                {
                    StatusCodeEnum.OK_200 => Ok(result),
                    StatusCodeEnum.NotFound_404 => NotFound(result),
                    _ => StatusCode(500, result)
                };
            });
        }

        // Lấy assignment kèm rubric/details
        [HttpGet("{id}/details")]
        [SwaggerOperation(
            Summary = "Lấy thông tin bài tập chi tiết kèm rubric",
            Description = "Trả về thông tin đầy đủ của bài tập bao gồm rubric và các chi tiết đánh giá"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AssignmentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài tập")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAssignmentWithDetails(int id)
        {
            return await CheckEnrollmentByAssignmentAndExecute(id, async () =>
            {
                var result = await _assignmentService.GetAssignmentWithDetailsAsync(id);
                return result.StatusCode switch
                {
                    StatusCodeEnum.OK_200 => Ok(result),
                    StatusCodeEnum.NotFound_404 => NotFound(result),
                    _ => StatusCode(500, result)
                };
            });
        }

        // Lấy assignment theo lớp học phần
        [HttpGet("course-instance/{courseInstanceId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài tập theo lớp học",
            Description = "Trả về tất cả bài tập thuộc một lớp học cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<AssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAssignmentsByCourseInstance(int courseInstanceId)
        {
            return await CheckEnrollmentAndExecute(courseInstanceId, async () =>
            {
                var result = await _assignmentService.GetAssignmentsByCourseInstanceAsync(courseInstanceId);
                return result.StatusCode switch
                {
                    StatusCodeEnum.OK_200 => Ok(result),
                    _ => StatusCode(500, result)
                };
            });
        }

        // Lấy assignment theo instructor
        [HttpGet("instructor/{instructorId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài tập theo giảng viên",
            Description = "Trả về tất cả bài tập được tạo bởi một giảng viên cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<AssignmentSummaryResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAssignmentsByInstructor(int instructorId)
        {
            var result = await _assignmentService.GetAssignmentsByInstructorAsync(instructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy assignment theo student
        [HttpGet("student/{studentId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài tập theo sinh viên",
            Description = "Trả về tất cả bài tập mà sinh viên cần thực hiện (dựa trên các lớp đã đăng ký)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<AssignmentSummaryResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetAssignmentsByStudent(int studentId)
        {
            // Verify the requested studentId matches the current user
            var currentStudentId = GetCurrentStudentId();
            if (studentId != currentStudentId)
            {
                return StatusCode(403, new BaseResponse<object>(
                    "Access denied: Cannot access other student's assignments",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var result = await _assignmentService.GetAssignmentsByStudentAsync(studentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy các assignment đang active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAssignments()
        {
            var result = await _assignmentService.GetActiveAssignmentsAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy các assignment bị overdue
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueAssignments()
        {
            var result = await _assignmentService.GetOverdueAssignmentsAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // Tạo assignment
        [HttpPost]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
        {
            var result = await _assignmentService.CreateAssignmentAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201
                    => CreatedAtAction(nameof(GetAssignmentById), new { id = result.Data?.AssignmentId }, result),
                _ => StatusCode(500, result)
            };
        }

        // Cập nhật assignment
        [HttpPut]
        public async Task<IActionResult> UpdateAssignment([FromBody] UpdateAssignmentRequest request)
        {
            var result = await _assignmentService.UpdateAssignmentAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Xóa assignment
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var result = await _assignmentService.DeleteAssignmentAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                StatusCodeEnum.Conflict_409 => Conflict(result),
                _ => StatusCode(500, result)
            };
        }

        // Gia hạn deadline
        [HttpPut("{id}/extend-deadline")]
        public async Task<IActionResult> ExtendDeadline(int id, [FromBody] DateTime newDeadline)
        {
            var result = await _assignmentService.ExtendDeadlineAsync(id, newDeadline);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Cập nhật rubric cho assignment
        [HttpPut("{id}/update-rubric/{rubricId}")]
        public async Task<IActionResult> UpdateRubric(int id, int rubricId)
        {
            var result = await _assignmentService.UpdateRubricAsync(id, rubricId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // Lấy thống kê assignment
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetAssignmentStatistics(int id)
        {
            var result = await _assignmentService.GetAssignmentStatisticsAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        private int GetCurrentStudentId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int studentId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return studentId;
        }
        private async Task<IActionResult> CheckEnrollmentAndExecute(int courseInstanceId, Func<Task<IActionResult>> action)
        {
            var studentId = GetCurrentStudentId();
            var enrollmentCheck = await _courseStudentService.IsStudentEnrolledAsync(courseInstanceId, studentId);
            if (!enrollmentCheck.Data)
            {
                return StatusCode(403, new BaseResponse<object>(
                    $"Access denied: {enrollmentCheck.Message}",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }
            return await action();
        }

        private async Task<IActionResult> CheckEnrollmentByAssignmentAndExecute(int assignmentId, Func<Task<IActionResult>> action)
        {
            var studentId = GetCurrentStudentId();

            // Get assignment to find course instance
            var assignmentResult = await _assignmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignmentResult.StatusCode != StatusCodeEnum.OK_200 || assignmentResult.Data == null)
            {
                return StatusCode((int)assignmentResult.StatusCode, assignmentResult);
            }

            var courseInstanceId = assignmentResult.Data.CourseInstanceId;
            var enrollmentCheck = await _courseStudentService.IsStudentEnrolledAsync(courseInstanceId, studentId);
            if (!enrollmentCheck.Data)
            {
                return StatusCode(403, new BaseResponse<object>(
                    $"Access denied: {enrollmentCheck.Message}",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }
            return await action();
        }

    }
}
