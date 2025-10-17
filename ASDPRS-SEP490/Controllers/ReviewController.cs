using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.Review;
using Service.RequestAndResponse.Response.Review;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Quản lý phân công review: tạo, phân công tự động, theo dõi tiến độ peer review")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Tạo review mới (peer review, instructor review, hoặc AI review)
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo review mới",
            Description = "Tạo một review mới (peer review, instructor review, hoặc AI review) với các feedback theo criteria"
        )]
        [SwaggerResponse(201, "Tạo review thành công", typeof(BaseResponse<ReviewResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy review assignment")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var response = await _reviewService.CreateReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Cập nhật review đã tồn tại
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật review",
            Description = "Cập nhật thông tin của review đã tồn tại, bao gồm điểm số và feedback"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<ReviewResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy review")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
        {
            var response = await _reviewService.UpdateReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        // Xóa review theo ReviewId
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa review",
            Description = "Xóa review theo ID. Chỉ có thể xóa review chưa được sử dụng trong tính điểm"
        )]
        [SwaggerResponse(200, "Xóa thành công", typeof(BaseResponse<bool>))]
        [SwaggerResponse(404, "Không tìm thấy review")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var response = await _reviewService.DeleteReviewAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy thông tin review chi tiết theo ReviewId
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin review chi tiết",
            Description = "Trả về thông tin đầy đủ của review theo ID, bao gồm các criteria feedback"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewResponse>))]
        [SwaggerResponse(404, "Không tìm thấy review")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            var response = await _reviewService.GetReviewByIdAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review theo ReviewAssignmentId
        [HttpGet("by-review-assignment/{reviewAssignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy review theo review assignment",
            Description = "Trả về tất cả review thuộc một review assignment cụ thể"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewsByReviewAssignmentId(int reviewAssignmentId)
        {
            var response = await _reviewService.GetReviewsByReviewAssignmentIdAsync(reviewAssignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review theo SubmissionId (tức là toàn bộ feedback của một bài nộp)
        [HttpGet("by-submission/{submissionId}")]
        [SwaggerOperation(
            Summary = "Lấy review theo submission",
            Description = "Trả về tất cả review của một bài nộp (toàn bộ feedback của submission)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewsBySubmissionId(int submissionId)
        {
            var response = await _reviewService.GetReviewsBySubmissionIdAsync(submissionId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review mà 1 reviewer (sinh viên hoặc giảng viên) đã làm
        [HttpGet("by-reviewer/{reviewerId}")]
        [SwaggerOperation(
            Summary = "Lấy review theo reviewer",
            Description = "Trả về tất cả review mà một reviewer (sinh viên hoặc giảng viên) đã thực hiện"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewsByReviewerId(int reviewerId)
        {
            var response = await _reviewService.GetReviewsByReviewerIdAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Lấy tất cả review trong 1 assignment (giảng viên dùng để xem tổng quan feedback)
        [HttpGet("by-assignment/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy review theo assignment",
            Description = "Trả về tất cả review trong một assignment (giảng viên dùng để xem tổng quan feedback)"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewsByAssignmentId(int assignmentId)
        {
            var response = await _reviewService.GetReviewsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Tạo review bởi AI (sẽ tự set FeedbackSource = "AI")
        [HttpPost("ai")]
        [SwaggerOperation(
            Summary = "Tạo review bằng AI",
            Description = "Tạo review tự động bằng AI, sẽ tự động set FeedbackSource = 'AI'"
        )]
        [SwaggerResponse(201, "Tạo AI review thành công", typeof(BaseResponse<ReviewResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> CreateAIReview([FromBody] CreateReviewRequest request)
        {
            var response = await _reviewService.CreateAIReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("submission/{submissionId}/statistics")]
        [SwaggerOperation(
    Summary = "Lấy thống kê review của submission",
    Description = "Trả về thống kê chi tiết về các review của một submission"
)]
        [SwaggerResponse(200, "Thành công", typeof(object))]
        [SwaggerResponse(404, "Không tìm thấy submission")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewStatisticsBySubmission(int submissionId)
        {
            var reviews = await _reviewService.GetReviewsBySubmissionIdAsync(submissionId);
            if (!reviews.StatusCode.ToString().StartsWith("2"))
            {
                return StatusCode((int)reviews.StatusCode, reviews);
            }

            var statistics = new
            {
                TotalReviews = reviews.Data?.Count ?? 0,
                AverageScore = reviews.Data?.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore.Value) ?? 0,
                PeerReviews = reviews.Data?.Count(r => r.ReviewType == "Peer") ?? 0,
                InstructorReviews = reviews.Data?.Count(r => r.ReviewType == "Instructor") ?? 0,
                AIReviews = reviews.Data?.Count(r => r.ReviewType == "AI") ?? 0
            };

            return Ok(new { Data = statistics, Message = "Statistics retrieved successfully" });
        }

        [HttpPut("{id}/criteria-feedback")]
        [SwaggerOperation(
            Summary = "Cập nhật criteria feedback của review",
            Description = "Cập nhật các feedback theo từng criteria của review"
        )]
        [SwaggerResponse(200, "Cập nhật thành công", typeof(BaseResponse<ReviewResponse>))]
        [SwaggerResponse(404, "Không tìm thấy review")]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> UpdateReviewCriteriaFeedback(int id, [FromBody] List<CriteriaFeedbackRequest> criteriaFeedbacks)
        {
            var request = new UpdateReviewRequest
            {
                ReviewId = id,
                CriteriaFeedbacks = criteriaFeedbacks
            };

            var response = await _reviewService.UpdateReviewAsync(request);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("assignment/{assignmentId}/summary")]
        [SwaggerOperation(
            Summary = "Lấy tổng quan review của assignment",
            Description = "Trả về tổng quan tất cả review trong một assignment"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        [SwaggerResponse(500, "Lỗi server")]
        public async Task<IActionResult> GetReviewSummaryByAssignment(int assignmentId)
        {
            var response = await _reviewService.GetReviewsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("completed/{reviewerId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách review đã hoàn thành",
            Description = "Trả về tất cả review mà reviewer đã hoàn thành, có thể chỉnh sửa nếu trong thời gian review"
        )]
        [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewResponse>>))]
        public async Task<IActionResult> GetCompletedReviewsByReviewer(int reviewerId)
        {
            var response = await _reviewService.GetCompletedReviewsByReviewerAsync(reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("completed/assignment/{assignmentId}/{reviewerId}")]
        [SwaggerOperation(
            Summary = "Lấy review đã hoàn thành theo assignment",
            Description = "Trả về review đã hoàn thành của reviewer trong assignment cụ thể"
        )]
        public async Task<IActionResult> GetCompletedReviewsByAssignment(int assignmentId, int reviewerId)
        {
            var response = await _reviewService.GetCompletedReviewsByAssignmentAsync(assignmentId, reviewerId);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
