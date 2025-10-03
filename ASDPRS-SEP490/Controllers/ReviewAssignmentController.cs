using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.ReviewAssignment;
using Service.RequestAndResponse.Response.ReviewAssignment;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý phân công review: tạo, phân công tự động, theo dõi tiến độ peer review")]
    public class ReviewAssignmentController : ControllerBase
    {
        private readonly IReviewAssignmentService _reviewAssignmentService;

        public ReviewAssignmentController(IReviewAssignmentService reviewAssignmentService)
        {
            _reviewAssignmentService = reviewAssignmentService;
        }

        // Tạo mới một review assignment
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo review assignment mới",
            Description = "Tạo mới một review assignment cho reviewer cụ thể"
        )]
        [SwaggerResponse(201, "Tạo thành công", typeof(BaseResponse<ReviewAssignmentResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy submission hoặc reviewer")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateReviewAssignment([FromBody] CreateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.CreateReviewAssignmentAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tạo nhiều review assignment cùng lúc cho 1 submission
        [HttpPost("bulk")]
        [SwaggerOperation(
            Summary = "Tạo nhiều review assignment cùng lúc",
            Description = "Tạo nhiều review assignment cho một submission với nhiều reviewer"
        )]
        [SwaggerResponse(201, "Tạo hàng loạt thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy submission hoặc reviewers")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> BulkCreateReviewAssignments([FromBody] BulkCreateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.BulkCreateReviewAssignmentsAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Cập nhật review assignment (status, deadline, IsAIReview)
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật review assignment",
            Description = "Cập nhật thông tin review assignment (status, deadline, IsAIReview)"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<ReviewAssignmentResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy review assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateReviewAssignment([FromBody] UpdateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.UpdateReviewAssignmentAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Xóa review assignment theo id
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa review assignment",
            Description = "Xóa review assignment theo ID. Chỉ có thể xóa khi chưa có review nào được tạo"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy review assignment")]
        [SwaggerResponse(409, "Không thể xóa do đã có review")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteReviewAssignment(int id)
        {
            var response = await _reviewAssignmentService.DeleteReviewAssignmentAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy chi tiết review assignment theo id
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết review assignment",
            Description = "Trả về thông tin chi tiết của review assignment theo ID"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewAssignmentResponse>))]
        [SwaggerResponse(404, "Không tìm thấy review assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewAssignmentById(int id)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentByIdAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment của một submission
        [HttpGet("by-submission/{submissionId}")]
        [SwaggerOperation(
            Summary = "Lấy review assignment theo submission",
            Description = "Trả về tất cả review assignment của một submission"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewAssignmentsBySubmissionId(int submissionId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment mà một reviewer được giao
        [HttpGet("by-reviewer/{reviewerId}")]
        [SwaggerOperation(
            Summary = "Lấy review assignment theo reviewer",
            Description = "Trả về tất cả review assignment mà một reviewer được giao"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewAssignmentsByReviewerId(int reviewerId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsByReviewerIdAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment của một assignment
        [HttpGet("by-assignment/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy review assignment theo assignment",
            Description = "Trả về tất cả review assignment của một assignment"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewAssignmentsByAssignmentId(int assignmentId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy danh sách review assignment quá hạn
        [HttpGet("overdue")]
        [SwaggerOperation(
            Summary = "Lấy review assignment quá hạn",
            Description = "Trả về danh sách review assignment đã quá deadline nhưng chưa hoàn thành"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetOverdueReviewAssignments()
        {
            var response = await _reviewAssignmentService.GetOverdueReviewAssignmentsAsync();
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy danh sách review assignment chưa hoàn thành của reviewer
        [HttpGet("pending/{reviewerId}")]
        [SwaggerOperation(
            Summary = "Lấy review assignment chưa hoàn thành",
            Description = "Trả về danh sách review assignment chưa hoàn thành của một reviewer"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetPendingReviewAssignments(int reviewerId)
        {
            var response = await _reviewAssignmentService.GetPendingReviewAssignmentsAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tự động phân công peer review cho assignment
        [HttpPost("auto-assign")]
        [SwaggerOperation(
            Summary = "Phân công peer review tự động",
            Description = "Tự động phân công peer review cho assignment với số lượng review mỗi submission được chỉ định"
        )]
        [SwaggerResponse(200, "Phân công tự động thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(400, "Số lượng review không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> AssignPeerReviewsAutomatically([FromQuery] int assignmentId, [FromQuery] int reviewsPerSubmission)
        {
            var response = await _reviewAssignmentService.AssignPeerReviewsAutomaticallyAsync(assignmentId, reviewsPerSubmission);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy thống kê peer review của một assignment
        [HttpGet("stats/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy thống kê peer review",
            Description = "Trả về thống kê chi tiết về peer review của một assignment"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<PeerReviewStatsResponse>))]
        [SwaggerResponse(404, "Không tìm thấy assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetPeerReviewStatistics(int assignmentId)
        {
            var response = await _reviewAssignmentService.GetPeerReviewStatisticsAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("student/{studentId}/completed")]
        [SwaggerOperation(
    Summary = "Lấy review assignment đã hoàn thành của sinh viên",
    Description = "Trả về danh sách các review assignment mà sinh viên đã hoàn thành"
)]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetCompletedReviewAssignments(int studentId)
        {
            var allAssignments = await _reviewAssignmentService.GetReviewAssignmentsByReviewerIdAsync(studentId);
            if (!allAssignments.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)allAssignments.StatusCode, allAssignments);
            }

            var completedAssignments = allAssignments.Data?.Where(ra => ra.Status == "Completed").ToList();
            return Ok(new BaseResponse<List<ReviewAssignmentResponse>>(
                "Completed review assignments retrieved successfully",
                Service.RequestAndResponse.Enums.StatusCodeEnum.OK_200,
                completedAssignments
            ));
        }

        [HttpGet("submission/{submissionId}/reviewers")]
        [SwaggerOperation(
    Summary = "Lấy danh sách reviewer của submission",
    Description = "Trả về danh sách tất cả reviewer được phân công cho một submission"
)]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewersBySubmission(int submissionId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
