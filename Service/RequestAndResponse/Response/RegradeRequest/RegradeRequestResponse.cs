using System;

namespace Service.RequestAndResponse.Response.RegradeRequest
{
    public class RegradeRequestResponse
    {
        public int RequestId { get; set; }
        public int SubmissionId { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public int? ReviewedByInstructorId { get; set; }
        public string ResolutionNotes { get; set; }

        // Navigation properties for detailed view
        public SubmissionInfoResponse Submission { get; set; }
        public UserInfoRegradeResponse RequestedByStudent { get; set; }
        public UserInfoRegradeResponse ReviewedByInstructor { get; set; }
        public AssignmentInfoRegradeResponse Assignment { get; set; }

        public decimal? CurrentScore { get; set; }       // điểm hiện tại trước khi regrade
        public decimal? UpdatedScore { get; set; }       // điểm sau khi regrade
        public string CourseName { get; set; }           // tên khóa học
        public string ClassName { get; set; }
        public GradeInfoResponse GradeInfo { get; set; }

        public string CourseName { get; set; }
        public string ClassName { get; set; }
    }

    public class SubmissionInfoResponse
    {
        public int SubmissionId { get; set; }
        public decimal? Score { get; set; }
        public string FileName { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; }

        public decimal? InstructorScore { get; set; }
        public decimal? PeerAverageScore { get; set; }
        public decimal? FinalScore { get; set; }
        public string Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
    }

    public class UserInfoRegradeResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class AssignmentInfoRegradeResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }
    }

    public class GradeInfoResponse
    {
        public decimal? FinalScoreAfterRegrade { get; set; }
        public decimal? InstructorScore { get; set; }
        public decimal? PeerAverageScore { get; set; }
        public string InstructorFeedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public bool HasBeenRegraded { get; set; }
        public string RegradeStatus { get; set; }
    }
}