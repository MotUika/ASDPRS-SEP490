using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Assignment;
using Service.RequestAndResponse.Response.Assignment;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý bài tập: CRUD, thống kê, gia hạn deadline, quản lý rubric, clone, tracking")]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        // ===================== CRUD =====================

        [HttpPost]
        [SwaggerOperation(Summary = "Tạo bài tập mới", Description = "Tạo mới một assignment cho lớp học phần")]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<AssignmentResponse>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateAssignment([FromForm] CreateAssignmentRequest request)
        {
            var result = await _assignmentService.CreateAssignmentAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 =>
                    CreatedAtAction(nameof(GetAssignmentById), new { id = result.Data?.AssignmentId }, result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut]
        [SwaggerOperation(Summary = "Cập nhật bài tập", Description = "Cập nhật thông tin bài tập hiện có")]
        public async Task<IActionResult> UpdateAssignment([FromForm] UpdateAssignmentRequest request)
        {
            var result = await _assignmentService.UpdateAssignmentAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Xóa bài tập", Description = "Xóa bài tập theo ID")]
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

        // ===================== GET =====================

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Lấy thông tin bài tập theo ID", Description = "Trả về thông tin chi tiết của bài tập dựa trên ID")]
        public async Task<IActionResult> GetAssignmentById(int id)
        {
            var result = await _assignmentService.GetAssignmentByIdAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("{id}/details")]
        [SwaggerOperation(Summary = "Lấy chi tiết bài tập kèm rubric", Description = "Trả về thông tin bài tập bao gồm rubric và các tiêu chí")]
        public async Task<IActionResult> GetAssignmentWithDetails(int id)
        {
            var result = await _assignmentService.GetAssignmentWithDetailsAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        // ===================== FILTER BY COURSE / USER =====================

        [HttpGet("course-instance/{courseInstanceId}")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập theo lớp học phần")]
        public async Task<IActionResult> GetAssignmentsByCourseInstance(int courseInstanceId)
        {
            var result = await _assignmentService.GetAssignmentsByCourseInstanceAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("instructor/{instructorId}")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập theo giảng viên")]
        public async Task<IActionResult> GetAssignmentsByInstructor(int instructorId)
        {
            var result = await _assignmentService.GetAssignmentsByInstructorAsync(instructorId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("student/{studentId}")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập theo sinh viên")]
        public async Task<IActionResult> GetAssignmentsByStudent(int studentId)
        {
            var result = await _assignmentService.GetAssignmentsByStudentAsync(studentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        // ===================== STATUS =====================

        [HttpGet("active")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập đang active")]
        public async Task<IActionResult> GetActiveAssignments()
        {
            var result = await _assignmentService.GetActiveAssignmentsAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("overdue")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập quá hạn")]
        public async Task<IActionResult> GetOverdueAssignments()
        {
            var result = await _assignmentService.GetOverdueAssignmentsAsync();
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut("{assignmentId}/publish")]
        [SwaggerOperation(Summary = "Publish bài tập", Description = "Chuyển bài tập từ trạng thái Draft sang Upcoming hoặc Active tùy theo StartDate")]
        [SwaggerResponse(200, "Publish thành công", typeof(BaseResponse<AssignmentResponse>))]
        [SwaggerResponse(400, "Bài tập không ở trạng thái Draft")]
        [SwaggerResponse(404, "Không tìm thấy bài tập")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> PublishAssignment(int assignmentId)
        {
            var result = await _assignmentService.PublishAssignmentAsync(assignmentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }


        // ===================== DEADLINE & RUBRIC =====================

        [HttpPut("{id}/extend-deadline")]
        [SwaggerOperation(Summary = "Gia hạn deadline cho bài tập")]
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

        [HttpPut("{id}/update-rubric/{rubricId}")]
        [SwaggerOperation(Summary = "Cập nhật rubric cho assignment")]
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

        [HttpGet("template/{templateId}/assignments")]
        [SwaggerOperation(
    Summary = "Lấy danh sách assignment đang sử dụng rubric template này",
    Description = "Trả về danh sách assignment có RubricTemplateId tương ứng."
)]
        [SwaggerResponse(200, "Danh sách assignment", typeof(BaseResponse<List<AssignmentResponse>>))]
        [SwaggerResponse(404, "Không tìm thấy rubric template")]
        public async Task<IActionResult> GetAssignmentsByRubricTemplate(int templateId)
        {
            var result = await _assignmentService.GetAssignmentsByRubricTemplateAsync(templateId);
            if (result.StatusCode == StatusCodeEnum.OK_200)
                return Ok(result);
            return StatusCode((int)result.StatusCode, result);
        }


        // ===================== STATISTICS =====================

        [HttpGet("{id}/stats")]
        [SwaggerOperation(Summary = "Lấy thống kê bài tập", Description = "Bao gồm số lượt nộp bài, điểm trung bình, v.v.")]
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

        // ===================== ADDITIONAL FEATURES =====================

        [HttpGet("{id}/rubric-for-review")]
        [SwaggerOperation(Summary = "Lấy rubric của bài tập để review")]
        public async Task<IActionResult> GetAssignmentRubricForReview(int id)
        {
            var result = await _assignmentService.GetAssignmentRubricForReviewAsync(id);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("course-instance/{courseInstanceId}/basic")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập cơ bản theo lớp học phần")]
        public async Task<IActionResult> GetAssignmentsByCourseInstanceBasic(int courseInstanceId)
        {
            var result = await _assignmentService.GetAssignmentsByCourseInstanceBasicAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("course-instance/{courseInstanceId}/active")]
        [SwaggerOperation(Summary = "Lấy danh sách bài tập đang active trong lớp học phần")]
        public async Task<IActionResult> GetActiveAssignmentsByCourseInstance(int courseInstanceId, [FromQuery] int? studentId = null)
        {
            var result = await _assignmentService.GetActiveAssignmentsByCourseInstanceAsync(courseInstanceId, studentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPost("{sourceAssignmentId}/clone/{targetCourseInstanceId}")]
        [SwaggerOperation(Summary = "Clone bài tập sang lớp học phần khác")]
        public async Task<IActionResult> CloneAssignment(int sourceAssignmentId, int targetCourseInstanceId, [FromBody] CloneAssignmentRequest request)
        {
            var result = await _assignmentService.CloneAssignmentAsync(sourceAssignmentId, targetCourseInstanceId, request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => Created("", result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpPut("{assignmentId}/timeline")]
        [SwaggerOperation(Summary = "Cập nhật timeline bài tập (StartDate, Deadline, ReviewDeadline)")]
        public async Task<IActionResult> UpdateAssignmentTimeline(int assignmentId, [FromBody] UpdateAssignmentTimelineRequest request)
        {
            var result = await _assignmentService.UpdateAssignmentTimelineAsync(assignmentId, request);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.BadRequest_400 => BadRequest(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("course-instance/{courseInstanceId}/status-summary")]
        [SwaggerOperation(Summary = "Lấy tổng hợp trạng thái bài tập trong lớp học phần")]
        public async Task<IActionResult> GetAssignmentStatusSummary(int courseInstanceId)
        {
            var result = await _assignmentService.GetAssignmentStatusSummaryAsync(courseInstanceId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }

        [HttpGet("{assignmentId}/tracking")]
        [SwaggerOperation(Summary = "Lấy tiến độ review/nộp bài của bài tập")]
        public async Task<IActionResult> GetAssignmentTracking(int assignmentId)
        {
            var result = await _assignmentService.GetAssignmentTrackingAsync(assignmentId);
            return result.StatusCode switch
            {
                StatusCodeEnum.OK_200 => Ok(result),
                StatusCodeEnum.NotFound_404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
    }
}
