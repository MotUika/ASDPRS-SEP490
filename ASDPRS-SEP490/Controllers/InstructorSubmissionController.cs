using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AISummary;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.AISummary;
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
        [SwaggerOperation(
    Summary = "Xem danh sách tất cả sinh viên (có nộp + chưa nộp) trong assignment",
    Description = "Trả về danh sách đầy đủ sinh viên trong lớp, bao gồm cả những người chưa nộp bài. " +
                  "Thông tin bao gồm: trạng thái nộp bài, điểm, file, và thông tin cá nhân.")]
        [SwaggerResponse(200, "Danh sách đầy đủ sinh viên với trạng thái nộp bài", typeof(BaseResponse<SubmissionListResponse>))]
        [SwaggerResponse(204, "Không có sinh viên nào trong lớp")]
        [SwaggerResponse(404, "Không tìm thấy assignment")]
        public async Task<IActionResult> GetSubmissionsByAssignment(int assignmentId)
        {
            var result = await _submissionService.GetSubmissionsAllStudentByAssignmentIdAsync(assignmentId);

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
        [SwaggerOperation(
            Summary = "Chấm điểm bài nộp",
            Description = "Giảng viên chấm điểm và ghi nhận feedback cho submission. Có thể chấm theo từng tiêu chí nếu assignment có rubric."
        )]
        [SwaggerResponse(200, "Chấm điểm thành công", typeof(BaseResponse<GradeSubmissionResponse>))]
        [SwaggerResponse(400, "Dữ liệu không hợp lệ hoặc thiếu thông tin")]
        [SwaggerResponse(404, "Không tìm thấy bài nộp hoặc assignment")]
        [SwaggerResponse(500, "Lỗi hệ thống trong quá trình chấm điểm")]
        public async Task<IActionResult> GradeSubmission([FromBody] GradeSubmissionRequest request)
        {
            // ✅ Kiểm tra đầu vào
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new BaseResponse<string>(
                    $"Invalid input: {string.Join("; ", errors)}",
                    StatusCodeEnum.BadRequest_400,
                    null
                ));
            }

            // ✅ Gọi service xử lý
            var result = await _submissionService.GradeSubmissionAsync(request);

            // ✅ Trả về kết quả theo status code
            return StatusCode((int)result.StatusCode, result);
        }


        [HttpPost("publish-grades")]
        [SwaggerOperation(
    Summary = "Công bố điểm cho Assignment",
    Description =
        "Kiểm tra điều kiện trước khi công bố:\n" +
        "- Đã chấm hết tất cả bài đã nộp\n" +
        "- Đã chấm 0 điểm cho sinh viên không nộp (nếu có)\n" +
        "- Qua FinalDeadline hoặc tỷ lệ nộp ≥ 50%\n\n" +
        "Dùng `ForcePublish = true` để bỏ qua tất cả điều kiện.\n\n" +
        "Trả về:\n" +
        "- Danh sách sinh viên + điểm cuối\n" +
        "- Tỷ lệ nộp / chấm\n" +
        "- Lý do chặn (nếu không thể public)"
)]
        [SwaggerResponse(200, "Công bố thành công hoặc thông báo trạng thái", typeof(BaseResponse<PublishGradesResponse>))]
        [SwaggerResponse(400, "Không thể công bố do còn bài chưa chấm / chưa nộp / chưa đến hạn")]
        [SwaggerResponse(404, "Không tìm thấy Assignment")]
        public async Task<IActionResult> PublishGrades([FromBody] PublishGradesRequest request)
        {
            if (request == null || request.AssignmentId <= 0)
                return BadRequest("Invalid request: AssignmentId is required.");

            var result = await _submissionService.PublishGradesAsync(request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("auto-grade-zero")]
        [SwaggerOperation(
    Summary = "Chấm 0 điểm tự động cho sinh viên KHÔNG NỘP bài",
    Description =
        "Chỉ thực hiện khi:\n" +
        "- Đã qua FinalDeadline\n" +
        "- `ConfirmZeroGrade = true`\n\n" +
        "Tạo bản ghi `Submission` với:\n" +
        "- `FinalScore = 0`\n" +
        "- `Status = Graded`\n" +
        "- `IsPublic = true`"
)]
        [SwaggerResponse(200, "Chấm 0 thành công", typeof(BaseResponse<AutoGradeZeroResponse>))]
        [SwaggerResponse(400, "Chưa xác nhận hoặc chưa đến hạn")]
        [SwaggerResponse(404, "Không tìm thấy Assignment")]
        public async Task<IActionResult> AutoGradeZero([FromBody] AutoGradeZeroRequest request)
        {
            if (request == null || request.AssignmentId <= 0)
                return BadRequest("AssignmentId và ConfirmZeroGrade là bắt buộc.");

            var result = await _submissionService.AutoGradeZeroForNonSubmittersAsync(request);
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

        [HttpPost("submission/{submissionId}/generate-overall-summary")]
        [SwaggerOperation(
                Summary = "Generate AI Overall Summary for submission (for instructor)",
                Description = "Tạo tóm tắt tổng quát AI cho bài nộp, dành cho giảng viên. Nếu chưa tồn tại, generate và lưu DB; nếu có, load từ DB."
            )]
        [SwaggerResponse(200, "Thành công (load existing)", typeof(BaseResponse<AIOverallResponse>))]
        [SwaggerResponse(201, "Thành công (generated mới)", typeof(BaseResponse<AIOverallResponse>))]
        [SwaggerResponse(400, "Yêu cầu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy bài nộp")]
        [SwaggerResponse(500, "Lỗi hệ thống")]
        public async Task<IActionResult> GenerateInstructorOverallSummary(int submissionId)
        {
            var request = new GenerateAIOverallRequest { SubmissionId = submissionId };
            var result = await _aiSummaryService.GenerateInstructorOverallSummaryAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("submission/{submissionId}/generate-criteria-feedback")]
        [SwaggerOperation(
            Summary = "Generate AI Criteria Feedback for submission (for instructor)",
            Description = "Tạo feedback AI theo từng tiêu chí cho bài nộp, dành cho giảng viên. Nếu chưa tồn tại, generate và lưu DB; nếu có, load từ DB."
        )]
        [SwaggerResponse(200, "Thành công (load existing)", typeof(BaseResponse<AICriteriaResponse>))]
        [SwaggerResponse(201, "Thành công (generated mới)", typeof(BaseResponse<AICriteriaResponse>))]
        [SwaggerResponse(400, "Yêu cầu không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy bài nộp hoặc rubric")]
        [SwaggerResponse(500, "Lỗi hệ thống")]
        public async Task<IActionResult> GenerateInstructorCriteriaFeedback(int submissionId)
        {
            var request = new GenerateAICriteriaRequest { SubmissionId = submissionId };
            var result = await _aiSummaryService.GenerateInstructorCriteriaFeedbackAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("submissions")]
        [SwaggerOperation(
            Summary = "Xem danh sách thông tin ",
            Description = "Lọc theo CourseId, ClassId hoặc AssignmentId. Chỉ giảng viên của lớp mới xem được.")]
        [SwaggerResponse(200, "Danh sách bài nộp", typeof(IEnumerable<InstructorSubmissionInfoResponse>))]
        [SwaggerResponse(403, "Không phải giảng viên của lớp")]
        [SwaggerResponse(404, "Không tìm thấy dữ liệu")]
        public async Task<IActionResult> GetInstructorSubmissionInfo(
            [FromQuery] int userId,
            [FromQuery] int? classId,
            [FromQuery] int? assignmentId)
        {
            try
            {
                var data = await _submissionService.GetInstructorSubmissionInfoAsync(userId, classId, assignmentId);

                if (data == null || !data.Any())
                {
                    return NotFound(new { Message = "Không tìm thấy dữ liệu bài nộp." });
                }

                return Ok(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Khi giảng viên không dạy lớp
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetInstructorSubmissionInfo: {ex.Message}");
                return StatusCode(500, new { Message = "Có lỗi xảy ra trên server.", Details = ex.Message });
            }
        }

        // 🔟 Lấy thông tin Submission đầy đủ dùng để export Excel
        [HttpGet("export/all/{assignmentId}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin chi tiết Submission để xuất Excel",
            Description = "Bao gồm userName, studentCode, assignment, submission, rubric criteria và điểm chấm theo tiêu chí."
        )]
        [SwaggerResponse(200, "Thông tin assignment đầy đủ", typeof(BaseResponse<SubmissionDetailExportResponse>))]
        [SwaggerResponse(404, "Không tìm thấy assignment")]
        public async Task<IActionResult> GetAllSubmissionDetailsForExportAsync(int assignmentId)
        {
            var result = await _submissionService.GetAllSubmissionDetailsForExportAsync(assignmentId);
            return StatusCode((int)result.StatusCode, result);
        }



        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportGradesExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            var result = await _submissionService.ImportGradesFromExcelAsync(file);
            return StatusCode((int)result.StatusCode, result);
        }


    }
}
