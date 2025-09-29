using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.ReviewAssignment;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewAssignmentController : ControllerBase
    {
        private readonly IReviewAssignmentService _reviewAssignmentService;

        public ReviewAssignmentController(IReviewAssignmentService reviewAssignmentService)
        {
            _reviewAssignmentService = reviewAssignmentService;
        }

        // Tạo mới một review assignment
        [HttpPost]
        public async Task<IActionResult> CreateReviewAssignment([FromBody] CreateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.CreateReviewAssignmentAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tạo nhiều review assignment cùng lúc cho 1 submission
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreateReviewAssignments([FromBody] BulkCreateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.BulkCreateReviewAssignmentsAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Cập nhật review assignment (status, deadline, IsAIReview)
        [HttpPut]
        public async Task<IActionResult> UpdateReviewAssignment([FromBody] UpdateReviewAssignmentRequest request)
        {
            var response = await _reviewAssignmentService.UpdateReviewAssignmentAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Xóa review assignment theo id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReviewAssignment(int id)
        {
            var response = await _reviewAssignmentService.DeleteReviewAssignmentAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy chi tiết review assignment theo id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewAssignmentById(int id)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentByIdAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment của một submission
        [HttpGet("by-submission/{submissionId}")]
        public async Task<IActionResult> GetReviewAssignmentsBySubmissionId(int submissionId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment mà một reviewer được giao
        [HttpGet("by-reviewer/{reviewerId}")]
        public async Task<IActionResult> GetReviewAssignmentsByReviewerId(int reviewerId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsByReviewerIdAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review assignment của một assignment
        [HttpGet("by-assignment/{assignmentId}")]
        public async Task<IActionResult> GetReviewAssignmentsByAssignmentId(int assignmentId)
        {
            var response = await _reviewAssignmentService.GetReviewAssignmentsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy danh sách review assignment quá hạn
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueReviewAssignments()
        {
            var response = await _reviewAssignmentService.GetOverdueReviewAssignmentsAsync();
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy danh sách review assignment chưa hoàn thành của reviewer
        [HttpGet("pending/{reviewerId}")]
        public async Task<IActionResult> GetPendingReviewAssignments(int reviewerId)
        {
            var response = await _reviewAssignmentService.GetPendingReviewAssignmentsAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tự động phân công peer review cho assignment
        [HttpPost("auto-assign")]
        public async Task<IActionResult> AssignPeerReviewsAutomatically([FromQuery] int assignmentId, [FromQuery] int reviewsPerSubmission)
        {
            var response = await _reviewAssignmentService.AssignPeerReviewsAutomaticallyAsync(assignmentId, reviewsPerSubmission);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy thống kê peer review của một assignment
        [HttpGet("stats/{assignmentId}")]
        public async Task<IActionResult> GetPeerReviewStatistics(int assignmentId)
        {
            var response = await _reviewAssignmentService.GetPeerReviewStatisticsAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
