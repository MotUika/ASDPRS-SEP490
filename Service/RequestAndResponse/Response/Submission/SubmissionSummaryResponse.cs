using System;

namespace Service.RequestAndResponse.Response.Submission
{
    public class SubmissionSummaryResponse
    {
        public int SubmissionId { get; set; }
        public int AssignmentId { get; set; }
        public int UserId { get; set; }

        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string StudentCode { get; set; }
        public string? CourseName { get; set; }
        public string? ClassName { get; set; }
        public string? AssignmentTitle { get; set; }
        public string AssignmentStatus { get; set; }
        public string? RegradeRequestStatus { get; set; }
        public DateTime SubmittedAt { get; set; }

        public decimal? PeerAverageScore { get; set; }
        public decimal? InstructorScore { get; set; }
        public decimal? FinalScore { get; set; }
        public string? Feedback { get; set; }
        public decimal InstructorWeight { get; set; }
        public decimal PeerWeight { get; set; }

        public string? Status { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
