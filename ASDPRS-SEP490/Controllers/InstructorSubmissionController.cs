using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.Review;
using Service.RequestAndResponse.Response.Submission;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace ASDPRS_SEP490.Controllers
{
    [ApiController]
    [Route("api/instructor/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Dành cho giảng viên: xem và chấm bài nộp của sinh viên")]
    public class InstructorSubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;
        private readonly IReviewService _reviewService;
        private readonly IAISummaryService _aiSummaryService;
        private readonly IAssignmentService _assignmentService;

        public InstructorSubmissionController(
            ISubmissionService submissionService,
            IReviewService reviewService,
            IAISummaryService aiSummaryService,
            IAssignmentService assignmentService)
        {
            _submissionService = submissionService;
            _reviewService = reviewService;
            _aiSummaryService = aiSummaryService;
            _assignmentService = assignmentService;
        }

        // 1️⃣ Xem danh sách bài nộp trong Assignment
        [HttpGet("assignment/{assignmentId}/submissions")]
        [SwaggerOperation(Summary = "Xem danh sách bài nộp trong assignment",
                          Description = "Trả về danh sách tất cả submissions của sinh viên trong 1 bài tập cụ thể.")]
        [SwaggerResponse(200, "Danh sách bài nộp", typeof(BaseResponse<object>))]
        public async Task<IActionResult> GetSubmissionsByAssignment(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionsByAssignmentIdAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        // 2️⃣ Xem chi tiết 1 bài nộp (gồm peer reviews + AI summaries)
        [HttpGet("{submissionId}/details")]
        [SwaggerOperation(Summary = "Xem chi tiết bài nộp",
                          Description = "Bao gồm thông tin bài nộp, peer reviews, AI summaries, và regrade requests.")]
        [SwaggerResponse(200, "Chi tiết bài nộp", typeof(BaseResponse<object>))]
        public async Task<IActionResult> GetSubmissionDetails(int submissionId)
        {
            var result = await _submissionService.GetSubmissionWithDetailsAsync(submissionId);
            return StatusCode((int)result.StatusCode, result);
        }

        // 3️⃣ Xem file bài nộp (File URL)
        [HttpGet("{submissionId}/file")]
        [SwaggerOperation(Summary = "Xem file bài nộp", Description = "Trả về đường dẫn file (FileUrl) để xem hoặc tải xuống.")]
        public async Task<IActionResult> GetSubmissionFile(int submissionId)
        {
            var result = await _submissionService.GetSubmissionByIdAsync(new()
            {
                SubmissionId = submissionId
            });

            if (result.StatusCode != StatusCodeEnum.OK_200 || result.Data == null)
                return StatusCode((int)result.StatusCode, result);

            return Ok(new
            {
                result.Data.SubmissionId,
                result.Data.FileUrl,
                result.Data.FileName,
                result.Data.OriginalFileName
            });
        }

        // 4️⃣ Xem tất cả peer reviews của 1 bài nộp
        [HttpGet("{submissionId}/peer-reviews")]
        [SwaggerOperation(Summary = "Xem tất cả peer reviews",
                          Description = "Trả về toàn bộ nhận xét và điểm của các sinh viên khác cho bài nộp này.")]
        public async Task<IActionResult> GetPeerReviews(int submissionId)
        {
            var result = await _reviewService.GetReviewsBySubmissionIdAsync(submissionId);
            return StatusCode((int)result.StatusCode, result);
        }

        // 5️⃣ Xem AI summaries (AI đánh giá tự động)
        //[HttpGet("{submissionId}/ai-summaries")]
        //[SwaggerOperation(Summary = "Xem AI summaries", Description = "Trả về bản tóm tắt hoặc đánh giá từ AI của bài nộp.")]
        //public async Task<IActionResult> GetAISummaries(int submissionId)
        //{
        //    var result = await _aiSummaryService.GetBySubmissionIdAsync(submissionId);
        //    return StatusCode((int)result.StatusCode, result);
        //}

        // 6️⃣ Xem thống kê submissions trong assignment
        [HttpGet("assignment/{assignmentId}/statistics")]
        [SwaggerOperation(Summary = "Thống kê submissions của assignment",
                          Description = "Bao gồm tổng số bài nộp, bài trễ, graded, pending, top keywords,...")]
        public async Task<IActionResult> GetAssignmentSubmissionStatistics(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionStatisticsAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        // 7️⃣ Chấm điểm (Instructor grading)
        [HttpPost("grade")]
        [SwaggerOperation(Summary = "Chấm điểm bài nộp", Description = "Instructor chấm điểm và ghi nhận feedback cho submission")]
        public async Task<IActionResult> GradeSubmission([FromBody] GradeSubmissionRequest request)
        {
            var result = await _submissionService.GradeSubmissionAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("publish-grades")]
        [SwaggerOperation(Summary = "Công bố điểm cho Assignment", Description = "Public toàn bộ điểm sau khi đủ 50% lớp hoặc qua deadline")]
        public async Task<IActionResult> PublishGrades([FromBody] PublishGradesRequest request)
        {
            var result = await _submissionService.PublishGradesAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }


        // 9️⃣ (Tuỳ chọn) Xem danh sách submissions chưa chấm (Pending)
        [HttpGet("assignment/{assignmentId}/pending")]
        [SwaggerOperation(Summary = "Xem bài nộp chưa chấm điểm",
                          Description = "Lọc các bài nộp có trạng thái 'Submitted' hoặc 'Late'.")]
        public async Task<IActionResult> GetPendingSubmissions(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionsByFilterAsync(new()
            {
                AssignmentId = assignmentId,
                Status = "Submitted"
            });
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary")]
        [SwaggerOperation(
    Summary = "Xem tổng hợp điểm (Peer + Instructor + Final)",
    Description = "Giảng viên xem tổng hợp điểm của sinh viên, có thể lọc theo Course, Class hoặc Assignment.")]
        [SwaggerResponse(200, "Danh sách điểm tổng hợp", typeof(BaseResponse<IEnumerable<SubmissionSummaryResponse>>))]
        [SwaggerResponse(204, "Không có dữ liệu")]
        [SwaggerResponse(500, "Lỗi hệ thống")]
        public async Task<IActionResult> GetSubmissionSummary(
    [FromQuery] int? courseId,
    [FromQuery] int? classId,
    [FromQuery] int? assignmentId)
        {
            var result = await _submissionService.GetSubmissionSummaryAsync(courseId, classId, assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }

        //[HttpGet("{submissionId}/peer-reviews")]
        //[ProducesResponseType(typeof(IEnumerable<ReviewResponse>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetPeerReviewsBySubmissionId(int submissionId)
        //{
        //    var result = await _reviewService.GetPeerReviewsBySubmissionIdAsync(submissionId);
        //    return Ok(result);
        //}


    }
}
