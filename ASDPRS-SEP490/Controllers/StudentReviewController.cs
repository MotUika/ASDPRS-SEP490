using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.IRepository;
using Service.Interface;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.AISummary;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Request.RegradeRequest;
using Service.RequestAndResponse.Request.Review;
using Service.RequestAndResponse.Request.Submission;
using Service.RequestAndResponse.Response.AISummary;
using Service.RequestAndResponse.Response.Assignment;
using Service.RequestAndResponse.Response.CourseInstance;
using Service.RequestAndResponse.Response.CourseStudent;
using Service.RequestAndResponse.Response.RegradeRequest;
using Service.RequestAndResponse.Response.Review;
using Service.RequestAndResponse.Response.ReviewAssignment;
using Service.RequestAndResponse.Response.Rubric;
using Service.RequestAndResponse.Response.Submission;
using Swashbuckle.AspNetCore.Annotations;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Luồng review cho sinh viên: xem bài tập cần review, thực hiện đánh giá peer review")]
public class StudentReviewController : ControllerBase
{
    private readonly ICourseStudentService _courseStudentService;
    private readonly IReviewAssignmentService _reviewAssignmentService;
    private readonly IReviewService _reviewService;
    private readonly IAssignmentService _assignmentService;
    private readonly ISubmissionService _submissionService;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IAISummaryService _aISummaryService;
    private readonly IRegradeRequestService _regradeRequestService;

    public StudentReviewController(
        ICourseStudentService courseStudentService,
        IReviewAssignmentService reviewAssignmentService,
        IReviewService reviewService,
        IAssignmentService assignmentService,
        ISubmissionService submissionService, IAssignmentRepository assignmentRepository, IAISummaryService aISummaryService, IRegradeRequestService regradeRequestService)
    {
        _courseStudentService = courseStudentService;
        _reviewAssignmentService = reviewAssignmentService;
        _reviewService = reviewService;
        _assignmentService = assignmentService;
        _submissionService = submissionService;
        _assignmentRepository = assignmentRepository;
        _aISummaryService = aISummaryService;
        _regradeRequestService = regradeRequestService;
    }


    [HttpGet("courses/{studentId}")]
    [SwaggerOperation(
            Summary = "Lấy danh sách lớp học của sinh viên",
            Description = "Trả về danh sách các lớp học mà sinh viên đã đăng ký và được active"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<MyCourseResponse>>))]
    [SwaggerResponse(404, "Không tìm thấy sinh viên")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetStudentCourses(int studentId)
    {
        var result = await _courseStudentService.GetStudentCoursesAsync(studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("course-instance/{courseInstanceId}/assignments")]
    [SwaggerOperation(
            Summary = "Lấy danh sách bài tập trong lớp học",
            Description = "Trả về danh sách bài tập trong một lớp học cụ thể mà sinh viên có thể review"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<AssignmentBasicResponse>>))]
    [SwaggerResponse(404, "Không tìm thấy lớp học")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetAssignmentsByCourseInstance(int courseInstanceId, int studentId)
    {
        var currentStudentId = GetCurrentStudentId();
        if (studentId != currentStudentId)
        {
            return StatusCode(403, new BaseResponse<object>(
                "Access denied: Cannot access other student's data",
                StatusCodeEnum.Forbidden_403,
                null
            ));
        }

        return await CheckEnrollmentAndExecute(courseInstanceId, async () =>
        {
            var result = await _assignmentService.GetActiveAssignmentsByCourseInstanceAsync(courseInstanceId, studentId);
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpGet("pending-reviews/{studentId}")]
    [SwaggerOperation(
            Summary = "Lấy danh sách bài cần review",
            Description = "Trả về danh sách các bài nộp mà sinh viên cần thực hiện peer review (có thể lọc theo lớp học)"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetPendingReviews(int studentId, [FromQuery] int? courseInstanceId = null)
    {
        var result = await _reviewAssignmentService.GetPendingReviewsForStudentAsync(studentId, courseInstanceId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("review-assignment/{reviewAssignmentId}/details")]
    [SwaggerOperation(
            Summary = "Lấy chi tiết bài cần review",
            Description = "Trả về thông tin chi tiết của một bài nộp cần review, bao gồm file bài nộp, rubric, và thông tin assignment"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewAssignmentDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy review assignment")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetReviewAssignmentDetails(int reviewAssignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetReviewAssignmentDetailsAsync(reviewAssignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/rubric")]
    [SwaggerOperation(
            Summary = "Lấy rubric của bài tập",
            Description = "Trả về rubric (bảng tiêu chí đánh giá) của bài tập để sinh viên sử dụng khi review"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RubricResponse>))]
    [SwaggerResponse(404, "Không tìm thấy bài tập hoặc rubric")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetAssignmentRubric(int assignmentId)
    {
        return await CheckEnrollmentByAssignmentAndExecute(assignmentId, async () =>
        {
            var result = await _assignmentService.GetAssignmentRubricForReviewAsync(assignmentId);
            if (result.StatusCode == StatusCodeEnum.OK_200 && result.Data != null)
            {
                var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
                result.Data.GradingScale = assignment?.GradingScale ?? "Scale10";
            }
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpPost("submit-review")]
    [SwaggerOperation(
            Summary = "Nộp bài review",
            Description = "Sinh viên nộp bài peer review với điểm số và feedback theo từng tiêu chí"
        )]
    [SwaggerResponse(201, "Nộp review thành công", typeof(BaseResponse<ReviewResponse>))]
    [SwaggerResponse(400, "Dữ liệu review không hợp lệ")]
    [SwaggerResponse(404, "Không tìm thấy review assignment")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> SubmitStudentReview([FromBody] SubmitStudentReviewRequest request)
    {
        var studentId = GetCurrentStudentId();
        request.ReviewerUserId = studentId;

        // Get review assignment to find submission and then assignment
        var reviewAssignmentResult = await _reviewAssignmentService.GetReviewAssignmentByIdAsync(request.ReviewAssignmentId);
        if (reviewAssignmentResult.StatusCode != StatusCodeEnum.OK_200 || reviewAssignmentResult.Data == null)
        {
            return StatusCode((int)reviewAssignmentResult.StatusCode, reviewAssignmentResult);
        }

        var submissionId = reviewAssignmentResult.Data.SubmissionId;
        var submissionRequest = new GetSubmissionByIdRequest { SubmissionId = submissionId };
        var submissionResult = await _submissionService.GetSubmissionByIdAsync(submissionRequest);
        if (submissionResult.StatusCode != StatusCodeEnum.OK_200 || submissionResult.Data == null)
        {
            return StatusCode((int)submissionResult.StatusCode, submissionResult);
        }

        var assignmentId = submissionResult.Data.AssignmentId;
        return await CheckEnrollmentByAssignmentAndExecute(assignmentId, async () =>
        {
            var result = await _reviewService.SubmitStudentReviewAsync(request);
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpGet("completed-reviews/{studentId}")]
    [SwaggerOperation(
    Summary = "Lấy review assignment đã hoàn thành",
    Description = "Trả về danh sách các review assignment mà sinh viên đã hoàn thành"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetCompletedReviews(int studentId)
    {
        var allAssignments = await _reviewAssignmentService.GetReviewAssignmentsByReviewerIdAsync(studentId);
        if (!allAssignments.StatusCode.ToString().StartsWith("2"))
        {
            return StatusCode((int)allAssignments.StatusCode, allAssignments);
        }

        var completedAssignments = allAssignments.Data?.Where(ra => ra.Status == "Completed").ToList();
        return Ok(new BaseResponse<List<ReviewAssignmentResponse>>(
            "Completed reviews retrieved successfully",
            Service.RequestAndResponse.Enums.StatusCodeEnum.OK_200,
            completedAssignments
        ));
    }

    [HttpGet("assignment/{assignmentId}/completed-reviews")]
    [SwaggerOperation(
        Summary = "Lấy list peer review đã hoàn thành của student trong assignment cụ thể",
        Description = "Trả về các review assignment đã hoàn thành (Completed) của student hiện tại trong assignment, bao gồm feedback per criteria"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    [SwaggerResponse(404, "Không tìm thấy review")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetCompletedReviewsByAssignment(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetCompletedReviewsByAssignmentAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("review/{reviewId}/details")]
    [SwaggerOperation(
    Summary = "Lấy chi tiết peer review đã hoàn thành theo ID",
    Description = "Trả về chi tiết review (bao gồm feedback per criteria) của student hiện tại, để edit hoặc xem"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewResponse>))]
    [SwaggerResponse(403, "Access denied")]
    [SwaggerResponse(404, "Không tìm thấy review")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetReviewDetails(int reviewId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewService.GetReviewDetailsAsync(reviewId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("all-classes/pending-reviews/{studentId}")]
    [SwaggerOperation(
    Summary = "Lấy tất cả bài cần review từ mọi lớp",
    Description = "Trả về danh sách tất cả bài nộp cần review từ tất cả các lớp mà sinh viên đang tham gia"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    [SwaggerResponse(404, "Không tìm thấy sinh viên hoặc không tham gia lớp nào")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetPendingReviewsAcrossAllClasses(int studentId)
    {
        var result = await _reviewAssignmentService.GetPendingReviewsAcrossAllClassesAsync(studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/random-review")]
    [SwaggerOperation(
        Summary = "Get random submission to review",
        Description = "Get a random submission from the assignment (excluding own submission and already reviewed)"
    )]
    [SwaggerResponse(200, "Success", typeof(BaseResponse<ReviewAssignmentDetailResponse>))]
    [SwaggerResponse(404, "No available submissions")]
    public async Task<IActionResult> GetRandomReview(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetRandomReviewAssignmentAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/random-cross-class-review")]
    [SwaggerOperation(
    Summary = "Get random cross-class submission to review",
    Description = "Get a random submission only from cross-class (other sections) for review, if eligible"
)]
    [SwaggerResponse(200, "Success", typeof(BaseResponse<ReviewAssignmentDetailResponse>))]
    [SwaggerResponse(404, "No available cross-class submissions")]
    [SwaggerResponse(400, "Not eligible for cross-class (e.g., not completed min in-class reviews)")]
    public async Task<IActionResult> GetRandomCrossClassReview(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        return await CheckEnrollmentByAssignmentAndExecute(assignmentId, async () =>
        {
            var result = await _reviewAssignmentService.GetRandomCrossClassReviewAssignmentAsync(assignmentId, studentId);
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpGet("assignment/{assignmentId}/available-reviews")]
    [SwaggerOperation(
        Summary = "Get all available submissions to review",
        Description = "Get all submissions that student can review (excluding own and already reviewed)"
    )]
    [SwaggerResponse(200, "Success", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    public async Task<IActionResult> GetAvailableReviews(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetAvailableReviewsForStudentAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("review/{reviewId}")]
    [SwaggerOperation(
        Summary = "Edit existing review",
        Description = "Edit a review that was previously submitted (only during review period)"
    )]
    [SwaggerResponse(200, "Review updated successfully", typeof(BaseResponse<ReviewResponse>))]
    [SwaggerResponse(403, "Cannot edit after review deadline")]
    public async Task<IActionResult> UpdateStudentReview(int reviewId, [FromBody] UpdateStudentReviewRequest request)
    {
        request.ReviewId = reviewId;
        request.ReviewerUserId = GetCurrentStudentId();

        var result = await _reviewService.UpdateStudentReviewAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("submission/{submissionId}/can-modify")]
    [SwaggerOperation(
        Summary = "Check if student can modify submission",
        Description = "Check if student can update or delete submission (before deadline)"
    )]
    [SwaggerResponse(200, "Success", typeof(BaseResponse<bool>))]
    public async Task<IActionResult> CanModifySubmission(int submissionId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _submissionService.CanStudentModifySubmissionAsync(submissionId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }
    [HttpGet("course-instance/{courseInstanceId}/assignments-with-tracking")]
    [Authorize]
    [SwaggerOperation(
    Summary = "Lấy danh sách bài tập trong lớp học kèm theo tracking review",
    Description = "Trả về danh sách bài tập trong một lớp học cụ thể kèm theo thông tin tracking review progress của sinh viên hiện tại"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<AssignmentBasicResponse>>))]
    [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
    [SwaggerResponse(404, "Không tìm thấy lớp học")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetAssignmentsWithTracking(int courseInstanceId)
    {
        var result = await _assignmentService.GetAssignmentsByCourseInstanceBasicAsync(courseInstanceId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/tracking")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Lấy thông tin tracking review cho bài tập cụ thể",
        Description = "Trả về thông tin chi tiết về tiến độ review của sinh viên hiện tại cho một bài tập cụ thể, bao gồm số review đã hoàn thành, số review còn lại, và trạng thái hoàn thành"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AssignmentTrackingResponse>))]
    [SwaggerResponse(401, "Unauthorized - Token không hợp lệ")]
    [SwaggerResponse(404, "Không tìm thấy bài tập")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetAssignmentTracking(int assignmentId)
    {
        var result = await _assignmentService.GetAssignmentTrackingAsync(assignmentId);
        return StatusCode((int)result.StatusCode, result);
    }
    private int GetCurrentStudentId()
    {
        var userIdClaim = User.FindFirst("userId");
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

    [HttpPost("submission/{submissionId}/generate-review")]
    [SwaggerOperation(
    Summary = "Tạo AI Review tổng quát cho bài nộp",
    Description = "Tạo review phù hợp mọi ngành học với đánh giá dựa trên nội dung bài và tiêu chí"
)]
    [SwaggerResponse(201, "Tạo review thành công", typeof(BaseResponse<AISummaryGenerationResponse>))]
    [SwaggerResponse(404, "Không tìm thấy bài nộp")]
    public async Task<IActionResult> GenerateEnhancedReview(int submissionId, [FromBody] bool replaceExisting = false)
    {
        try
        {
            var studentId = GetCurrentStudentId();

            // Kiểm tra quyền
            var reviewAssignments = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            var canReview = reviewAssignments.Data?.Any(ra => ra.ReviewerUserId == studentId) ?? false;

            if (!canReview)
            {
                return StatusCode(403, new BaseResponse<EnhancedReviewResponse>(
                    "Access denied: You are not assigned to review this submission",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var aiSummaryService = HttpContext.RequestServices.GetService<IAISummaryService>();
            if (aiSummaryService == null)
            {
                return StatusCode(500, new BaseResponse<EnhancedReviewResponse>(
                    "AI Summary service not available",
                    StatusCodeEnum.InternalServerError_500,
                    null
                ));
            }

            var request = new GenerateReviewRequest
            {
                SubmissionId = submissionId,
                ReplaceExisting = replaceExisting
            };

            var result = await aiSummaryService.GenerateEnhancedReviewAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<EnhancedReviewResponse>(
                $"Error generating enhanced AI review: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }
    [HttpGet("submission/{submissionId}/Total-AI-review")]
    [SwaggerOperation(
        Summary = "Lấy AI Review tổng quát của bài nộp",
        Description = "Lấy review AI phù hợp mọi ngành học cho bài nộp đang review"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<AISummaryResponse>))]
    [SwaggerResponse(404, "Không tìm thấy review")]
    public async Task<IActionResult> GetUniversalReview(int submissionId)
    {
        try
        {
            var studentId = GetCurrentStudentId();

            // Kiểm tra quyền
            var reviewAssignments = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            var canReview = reviewAssignments.Data?.Any(ra => ra.ReviewerUserId == studentId) ?? false;

            if (!canReview)
            {
                return StatusCode(403, new BaseResponse<AISummaryResponse>(
                    "Access denied: You are not assigned to review this submission",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var aiSummaryService = HttpContext.RequestServices.GetService<IAISummaryService>();
            if (aiSummaryService == null)
            {
                return StatusCode(500, new BaseResponse<AISummaryResponse>(
                    "AI Summary service not available",
                    StatusCodeEnum.InternalServerError_500,
                    null
                ));
            }

            // Lấy review tổng quát
            var result = await aiSummaryService.GetAISummaryBySubmissionAndTypeAsync(submissionId, "UniversalReview");
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<AISummaryResponse>(
                $"Error retrieving universal AI review: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }
    [HttpPost("submission/{submissionId}/ai-overall-summary")]
    [SwaggerOperation(
        Summary = "Generate or load AI Overall Summary for submission",
        Description = "Nếu chưa tồn tại, generate tóm tắt tổng quát ~100 từ và lưu DB; nếu có, load từ DB."
    )]
    [SwaggerResponse(200, "Thành công (load hoặc generate)", typeof(BaseResponse<AIOverallResponse>))]
    [SwaggerResponse(403, "Access denied")]
    [SwaggerResponse(404, "Không tìm thấy submission")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GenerateAIOverallSummary(int submissionId)
    {
        try
        {
            var studentId = GetCurrentStudentId();

            // Kiểm tra quyền (tương tự existing)
            var reviewAssignments = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            var canReview = reviewAssignments.Data?.Any(ra => ra.ReviewerUserId == studentId) ?? false;

            if (!canReview)
            {
                return StatusCode(403, new BaseResponse<AIOverallResponse>(
                    "Access denied: You are not assigned to review this submission",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var request = new GenerateAIOverallRequest { SubmissionId = submissionId };
            var result = await _aISummaryService.GenerateOverallSummaryAsync(request);  // Method đã modify để lưu
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<AIOverallResponse>(
                $"Error generating/loading AI overall summary: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }

    [HttpPost("submission/{submissionId}/ai-criteria-feedback")]
    [SwaggerOperation(
        Summary = "Generate or load AI Criteria Feedback for submission",
        Description = "Nếu chưa tồn tại, generate feedback theo từng criteria và lưu DB; nếu có, load từ DB."
    )]
    [SwaggerResponse(200, "Thành công (load hoặc generate)", typeof(BaseResponse<AICriteriaResponse>))]
    [SwaggerResponse(403, "Access denied")]
    [SwaggerResponse(404, "Không tìm thấy submission hoặc rubric")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GenerateAICriteriaFeedback(int submissionId)
    {
        try
        {
            var studentId = GetCurrentStudentId();

            // Kiểm tra quyền (tương tự)
            var reviewAssignments = await _reviewAssignmentService.GetReviewAssignmentsBySubmissionIdAsync(submissionId);
            var canReview = reviewAssignments.Data?.Any(ra => ra.ReviewerUserId == studentId) ?? false;

            if (!canReview)
            {
                return StatusCode(403, new BaseResponse<AICriteriaResponse>(
                    "Access denied: You are not assigned to review this submission",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var request = new GenerateAICriteriaRequest { SubmissionId = submissionId };
            var result = await _aISummaryService.GenerateCriteriaFeedbackAsync(request);  // Method đã modify để lưu
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<AICriteriaResponse>(
                $"Error generating/loading AI criteria feedback: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }

    [HttpGet("assignment/{assignmentId}/user/{userId}")]
    [SwaggerOperation(
    Summary = "Lấy bài nộp của user trong assignment cụ thể",
    Description = "Trả về bài nộp của một user cụ thể trong assignment cụ thể"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<SubmissionResponse>))]
    [SwaggerResponse(404, "Không tìm thấy bài nộp")]
    public async Task<IActionResult> GetSubmissionByAssignmentAndUser(int assignmentId, int userId)
    {
        return await CheckEnrollmentByAssignmentAndExecute(assignmentId, async () =>
        {
            var result = await _submissionService.GetSubmissionByAssignmentAndUserAsync(assignmentId, userId);
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpGet("course-instance/{courseInstanceId}/user/{userId}")]
    [SwaggerOperation(
        Summary = "Lấy tất cả bài nộp của user trong lớp học cụ thể",
        Description = "Trả về tất cả bài nộp của một user trong một lớp học (course instance) cụ thể"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<SubmissionResponse>>))]
    public async Task<IActionResult> GetSubmissionsByCourseInstanceAndUser(int courseInstanceId, int userId)
    {
        return await CheckEnrollmentAndExecute(courseInstanceId, async () =>
        {
            var result = await _submissionService.GetSubmissionsByCourseInstanceAndUserAsync(courseInstanceId, userId);
            return StatusCode((int)result.StatusCode, result);
        });
    }

    [HttpGet("user/{userId}/semester/{semesterId}")]
    [SwaggerOperation(
        Summary = "Lấy tất cả bài nộp của user theo kỳ học",
        Description = "Trả về tất cả bài nộp của một user được filter theo kỳ học (semester)"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<SubmissionResponse>>))]
    public async Task<IActionResult> GetSubmissionsByUserAndSemester(int userId, int semesterId)
    {
        try
        {
            // Kiểm tra quyền truy cập - chỉ cho phép xem bài nộp của chính mình
            var currentStudentId = GetCurrentStudentId();
            if (userId != currentStudentId)
            {
                return StatusCode(403, new BaseResponse<object>(
                    "Access denied: Cannot access other student's submissions",
                    StatusCodeEnum.Forbidden_403,
                    null
                ));
            }

            var result = await _submissionService.GetSubmissionsByUserAndSemesterAsync(userId, semesterId);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(401, new BaseResponse<object>(
                ex.Message,
                StatusCodeEnum.Unauthorized_401,
                null
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<object>(
                $"Error retrieving submissions: {ex.Message}",
                StatusCodeEnum.InternalServerError_500,
                null
            ));
        }
    }

    [HttpGet("assignment/{assignmentId}/my-score")]
    [SwaggerOperation(
        Summary = "Lấy điểm final score của sinh viên cho assignment",
        Description = "Trả về final score của assignment cho sinh viên hiện tại, chỉ khi assignment đã publish grades"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<decimal?>))]
    [SwaggerResponse(403, "Access denied hoặc chưa publish")]
    [SwaggerResponse(404, "Không tìm thấy submission")]
    public async Task<IActionResult> GetMyScore(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _submissionService.GetMyScoreAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/my-score-details")]
    [SwaggerOperation(
        Summary = "Lấy chi tiết điểm của sinh viên cho assignment",
        Description = "Trả về instructor score, peer average, final score, feedback, và info khiếu nại cho assignment của sinh viên hiện tại"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<MyScoreDetailsResponse>))]
    [SwaggerResponse(403, "Access denied hoặc chưa publish")]
    [SwaggerResponse(404, "Không tìm thấy submission")]
    public async Task<IActionResult> GetMyScoreDetails(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _submissionService.GetMyScoreDetailsAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("my-regrade-history")]
    [SwaggerOperation(
        Summary = "Lấy lịch sử khiếu nại toàn bộ của sinh viên",
        Description = "Trả về danh sách tất cả regrade requests của sinh viên hiện tại"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
    [SwaggerResponse(404, "Không tìm thấy")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetMyRegradeHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var studentId = GetCurrentStudentId();
        var result = await _regradeRequestService.GetRegradeRequestsByStudentIdAsync(studentId, pageNumber, pageSize);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/my-regrade-history")]
    [SwaggerOperation(
        Summary = "Lấy lịch sử khiếu nại của sinh viên trong assignment cụ thể",
        Description = "Trả về danh sách regrade requests của sinh viên hiện tại cho assignment cụ thể"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<RegradeRequestListResponse>))]
    [SwaggerResponse(404, "Không tìm thấy")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetMyRegradeHistoryByAssignment(int assignmentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var studentId = GetCurrentStudentId();
        var filterRequest = new GetRegradeRequestsByFilterRequest
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _regradeRequestService.GetRegradeRequestsByFilterAsync(filterRequest);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("assignment/{assignmentId}/check-plagiarism")]
    [SwaggerOperation(
        Summary = "Kiểm tra tỷ lệ trùng lặp bài nộp (chủ động)",
        Description = "Sinh viên upload file để check tỷ lệ similarity với các bài khác trong assignment, không lưu file."
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<PlagiarismCheckResponse>))]
    [SwaggerResponse(403, "Access denied")]
    [SwaggerResponse(404, "Không tìm thấy assignment")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> CheckPlagiarism(int assignmentId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new BaseResponse<PlagiarismCheckResponse>("No file uploaded", StatusCodeEnum.BadRequest_400, null));
        }

        var studentId = GetCurrentStudentId();
        var result = await _submissionService.CheckPlagiarismActiveAsync(assignmentId, file, studentId);
        return StatusCode((int)result.StatusCode, result);
    }
}