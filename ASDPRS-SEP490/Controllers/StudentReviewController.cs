using Microsoft.AspNetCore.Mvc;
using Service.IService;
using Service.RequestAndResponse.Request.CourseStudent;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetStudentCourses(int studentId)
    {
        var result = await _courseStudentService.GetStudentCoursesAsync(studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("course-instance/{courseInstanceId}/assignments")]
    public async Task<IActionResult> GetAssignmentsByCourseInstance(int courseInstanceId, int studentId)
    {
        var result = await _assignmentService.GetAssignmentsByCourseInstanceBasicAsync(courseInstanceId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("pending-reviews/{studentId}")]
    public async Task<IActionResult> GetPendingReviews(int studentId, [FromQuery] int? courseInstanceId = null)
    {
        var result = await _reviewAssignmentService.GetPendingReviewsForStudentAsync(studentId, courseInstanceId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("review-assignment/{reviewAssignmentId}/details")]
    public async Task<IActionResult> GetReviewAssignmentDetails(int reviewAssignmentId)
    {
        var studentId = GetCurrentStudentId();
        var result = await _reviewAssignmentService.GetReviewAssignmentDetailsAsync(reviewAssignmentId, studentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("assignment/{assignmentId}/rubric")]
    public async Task<IActionResult> GetAssignmentRubric(int assignmentId)
    {
        var result = await _assignmentService.GetAssignmentRubricForReviewAsync(assignmentId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("submit-review")]
    public async Task<IActionResult> SubmitStudentReview([FromBody] SubmitStudentReviewRequest request)
    {
        var studentId = GetCurrentStudentId();
        request.ReviewerUserId = studentId;

        var result = await _reviewService.SubmitStudentReviewAsync(request);
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