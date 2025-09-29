using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.Review;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Tạo review mới (peer review, instructor review, hoặc AI review)
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var response = await _reviewService.CreateReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Cập nhật review đã tồn tại
        [HttpPut]
        public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
        {
            var response = await _reviewService.UpdateReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Xóa review theo ReviewId
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var response = await _reviewService.DeleteReviewAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy thông tin review chi tiết theo ReviewId
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            var response = await _reviewService.GetReviewByIdAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review theo ReviewAssignmentId
        [HttpGet("by-review-assignment/{reviewAssignmentId}")]
        public async Task<IActionResult> GetReviewsByReviewAssignmentId(int reviewAssignmentId)
        {
            var response = await _reviewService.GetReviewsByReviewAssignmentIdAsync(reviewAssignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review theo SubmissionId (tức là toàn bộ feedback của một bài nộp)
        [HttpGet("by-submission/{submissionId}")]
        public async Task<IActionResult> GetReviewsBySubmissionId(int submissionId)
        {
            var response = await _reviewService.GetReviewsBySubmissionIdAsync(submissionId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review mà 1 reviewer (sinh viên hoặc giảng viên) đã làm
        [HttpGet("by-reviewer/{reviewerId}")]
        public async Task<IActionResult> GetReviewsByReviewerId(int reviewerId)
        {
            var response = await _reviewService.GetReviewsByReviewerIdAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review trong 1 assignment (giảng viên dùng để xem tổng quan feedback)
        [HttpGet("by-assignment/{assignmentId}")]
        public async Task<IActionResult> GetReviewsByAssignmentId(int assignmentId)
        {
            var response = await _reviewService.GetReviewsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tạo review bởi AI (sẽ tự set FeedbackSource = "AI")
        [HttpPost("ai")]
        public async Task<IActionResult> CreateAIReview([FromBody] CreateReviewRequest request)
        {
            var response = await _reviewService.CreateAIReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
