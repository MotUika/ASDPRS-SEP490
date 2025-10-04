using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.Submission;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý bài nộp của sinh viên: tạo, cập nhật, xem chi tiết, thống kê")]
    public class SubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Tạo submission mới
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo bài nộp mới",
            Description = "Sinh viên nộp file bài làm lần đầu cho một Assignment cụ thể"
        )]
        [SwaggerResponse(201, "Tạo bài nộp thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc lỗi upload")]
        [SwaggerResponse(404, "Không tìm thấy Assignment hoặc User")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateSubmission([FromForm] CreateSubmissionRequest request)
        {
            var result = await _submissionService.CreateSubmissionAsync(request);
            return result.StatusCode switch
            {
                StatusCodeEnum.Created_201 => CreatedAtAction(nameof(GetSubmissionById), new { id = result.Data?.SubmissionId }, result),
                _ => StatusCode((int)result.StatusCode, result)
            };
        }

        // Sinh viên nộp bài (flow khác với CreateSubmission)
        [HttpPost("submit")]
        [SwaggerOperation(
            Summary = "Sinh viên nộp bài",
            Description = "Dành cho sinh viên nộp bài làm (tương tự CreateSubmission nhưng flow rút gọn)"
        )]
        [SwaggerResponse(200, "Nộp bài thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(400, "File hoặc dữ liệu không hợp lệ")]
        public async Task<IActionResult> SubmitAssignment([FromForm] SubmitAssignmentRequest request)
        {
            var result = await _submissionService.SubmitAssignmentAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submission theo ID
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin bài nộp theo ID",
            Description = "Trả về chi tiết bài nộp, có thể bao gồm review và AI summaries nếu chỉ định"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        public async Task<IActionResult> GetSubmissionById(int id, [FromQuery] bool includeReviews = false, [FromQuery] bool includeAISummaries = false)
        {
            var request = new GetSubmissionByIdRequest
            {
                SubmissionId = id,
                IncludeReviews = includeReviews,
                IncludeAISummaries = includeAISummaries
            };

            var result = await _submissionService.GetSubmissionByIdAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submissions theo filter
        [HttpGet("filter")]
        [SwaggerOperation(
            Summary = "Lọc danh sách bài nộp",
            Description = "Tìm kiếm và lọc bài nộp theo Assignment, User, Status, ngày nộp, công khai, v.v."
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionListResponse>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetSubmissionsByFilter([FromQuery] GetSubmissionsByFilterRequest request)
        {
            var result = await _submissionService.GetSubmissionsByFilterAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật submission
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật bài nộp",
            Description = "Cho phép sinh viên hoặc giảng viên cập nhật file, từ khóa hoặc trạng thái của bài nộp"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateSubmission([FromForm] UpdateSubmissionRequest request)
        {
            var result = await _submissionService.UpdateSubmissionAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật trạng thái submission
        [HttpPut("status")]
        [SwaggerOperation(
            Summary = "Cập nhật trạng thái bài nộp",
            Description = "Cập nhật trạng thái như Submitted, Graded, Late, v.v."
        )]
        [SwaggerResponse(200, "Cập nhật trạng thái thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        public async Task<IActionResult> UpdateSubmissionStatus([FromBody] UpdateSubmissionStatusRequest request)
        {
            var result = await _submissionService.UpdateSubmissionStatusAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Xóa submission
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa bài nộp",
            Description = "Xóa bài nộp cùng file lưu trữ tương ứng khỏi hệ thống"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            var result = await _submissionService.DeleteSubmissionAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submissions theo assignment
        [HttpGet("assignment/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài nộp theo Assignment",
            Description = "Trả về danh sách bài nộp thuộc về một Assignment cụ thể, hỗ trợ phân trang"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionListResponse>))]
        public async Task<IActionResult> GetSubmissionsByAssignmentId(int assignmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _submissionService.GetSubmissionsByAssignmentIdAsync(assignmentId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submissions theo user
        [HttpGet("user/{userId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài nộp theo User",
            Description = "Trả về các bài nộp của một sinh viên cụ thể, hỗ trợ phân trang"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionListResponse>))]
        public async Task<IActionResult> GetSubmissionsByUserId(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _submissionService.GetSubmissionsByUserIdAsync(userId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        // Thống kê submissions của assignment
        [HttpGet("statistics/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Thống kê bài nộp của Assignment",
            Description = "Lấy thống kê tổng số bài nộp, số bài trễ, trạng thái và từ khóa phổ biến"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionStatisticsResponse>))]
        [SwaggerResponse(404, "Không tìm thấy Assignment")]
        public async Task<IActionResult> GetSubmissionStatistics(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionStatisticsAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Kiểm tra user đã nộp bài chưa
        [HttpGet("exists")]
        [SwaggerOperation(
            Summary = "Kiểm tra bài nộp tồn tại",
            Description = "Kiểm tra sinh viên đã nộp bài cho Assignment này hay chưa"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<bool>))]
        public async Task<IActionResult> CheckSubmissionExists([FromQuery] int assignmentId, [FromQuery] int userId)
        {
            var result = await _submissionService.CheckSubmissionExistsAsync(assignmentId, userId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submission kèm chi tiết (review, AI summary)/ not yet 
        [HttpGet("details/{submissionId}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết bài nộp đầy đủ",
            Description = "Bao gồm thông tin bài nộp, review, AI summaries và regrade requests (nếu có)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionResponse>))]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        public async Task<IActionResult> GetSubmissionWithDetails(int submissionId)
        {
            var result = await _submissionService.GetSubmissionWithDetailsAsync(submissionId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
