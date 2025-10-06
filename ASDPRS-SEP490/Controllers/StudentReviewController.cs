using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.CourseStudent;
using Service.RequestAndResponse.Response.Assignment;
using Service.RequestAndResponse.Response.CourseInstance;
using Service.RequestAndResponse.Response.Review;
using Service.RequestAndResponse.Response.ReviewAssignment;
using Service.RequestAndResponse.Response.Rubric;
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

    public StudentReviewController(
        ICourseStudentService courseStudentService,
        IReviewAssignmentService reviewAssignmentService,
        IReviewService reviewService,
        IAssignmentService assignmentService)
    {
        _courseStudentService = courseStudentService;
        _reviewAssignmentService = reviewAssignmentService;
        _reviewService = reviewService;
        _assignmentService = assignmentService;
    }

    [HttpGet("courses/{studentId}")]
    [SwaggerOperation(
            Summary = "Lấy danh sách lớp học của sinh viên",
            Description = "Trả về danh sách các lớp học mà sinh viên đã đăng ký và được active"
        )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<CourseInstanceResponse>>))]
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
        var result = await _assignmentService.GetActiveAssignmentsByCourseInstanceAsync(courseInstanceId, studentId);
        return StatusCode((int)result.StatusCode, result);
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

    [HttpGet("assignment/{assignmentId}/random-pending")]
    [SwaggerOperation(
    Summary = "Lấy bài cần review ngẫu nhiên trong assignment",
    Description = "Trả về một bài nộp ngẫu nhiên trong assignment mà sinh viên có thể review"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewAssignmentDetailResponse>))]
    [SwaggerResponse(404, "Không có bài nào để review")]
    public async Task<IActionResult> GetRandomPendingReview(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetRandomPendingReviewByAssignmentAsync(assignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/pending-reviews")]
    [SwaggerOperation(
        Summary = "Lấy danh sách bài cần review trong assignment",
        Description = "Trả về tất cả bài nộp trong assignment mà sinh viên cần review"
    )]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<List<ReviewAssignmentResponse>>))]
    public async Task<IActionResult> GetPendingReviewsByAssignment(int assignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetPendingReviewsByAssignmentAsync(assignmentId, studentId);
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
        var result = await _assignmentService.GetAssignmentRubricForReviewAsync(assignmentId);
        return StatusCode((int)result.StatusCode, result);
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

        var result = await _reviewService.SubmitStudentReviewAsync(request);
        return StatusCode((int)result.StatusCode, result);
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

    [HttpGet("review-assignment/{reviewAssignmentId}/status")]
    [SwaggerOperation(
    Summary = "Kiểm tra trạng thái review assignment",
    Description = "Kiểm tra xem review assignment đã được hoàn thành chưa và thông tin cơ bản"
)]
    [SwaggerResponse(200, "Thành công", typeof(BaseResponse<ReviewAssignmentResponse>))]
    [SwaggerResponse(404, "Không tìm thấy review assignment")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> GetReviewAssignmentStatus(int reviewAssignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetReviewAssignmentByIdAsync(reviewAssignmentId);
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
}