using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Submission;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Tạo submission mới
        [HttpPost]
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
        public async Task<IActionResult> SubmitAssignment([FromForm] SubmitAssignmentRequest request)
        {
            var result = await _submissionService.SubmitAssignmentAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submission theo ID
        [HttpGet("{id}")]
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
        public async Task<IActionResult> GetSubmissionsByFilter([FromQuery] GetSubmissionsByFilterRequest request)
        {
            var result = await _submissionService.GetSubmissionsByFilterAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật submission
        [HttpPut]
        public async Task<IActionResult> UpdateSubmission([FromForm] UpdateSubmissionRequest request)
        {
            var result = await _submissionService.UpdateSubmissionAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Cập nhật trạng thái submission
        [HttpPut("status")]
        public async Task<IActionResult> UpdateSubmissionStatus([FromBody] UpdateSubmissionStatusRequest request)
        {
            var result = await _submissionService.UpdateSubmissionStatusAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        // Xóa submission
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            var result = await _submissionService.DeleteSubmissionAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submissions theo assignment
        [HttpGet("assignment/{assignmentId}")]
        public async Task<IActionResult> GetSubmissionsByAssignmentId(int assignmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _submissionService.GetSubmissionsByAssignmentIdAsync(assignmentId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submissions theo user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetSubmissionsByUserId(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _submissionService.GetSubmissionsByUserIdAsync(userId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        // Thống kê submissions của assignment
        [HttpGet("statistics/{assignmentId}")]
        public async Task<IActionResult> GetSubmissionStatistics(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionStatisticsAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Kiểm tra user đã nộp bài chưa
        [HttpGet("exists")]
        public async Task<IActionResult> CheckSubmissionExists([FromQuery] int assignmentId, [FromQuery] int userId)
        {
            var result = await _submissionService.CheckSubmissionExistsAsync(assignmentId, userId);
            return StatusCode((int)result.StatusCode, result);
        }

        // Lấy submission kèm chi tiết (review, AI summary)/ not yet 
        [HttpGet("details/{submissionId}")]
        public async Task<IActionResult> GetSubmissionWithDetails(int submissionId)
        {
            var result = await _submissionService.GetSubmissionWithDetailsAsync(submissionId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
