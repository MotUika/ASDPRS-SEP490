using Service.RequestAndResponse.Response.AISummary;
using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class SubmissionResponse
    {
        public int SubmissionId { get; set; }
        public int AssignmentId { get; set; }
        public int UserId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public string? CourseName { get; set; }
        public string? ClassName { get; set; }

        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string Keywords { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; }
        public bool IsPublic { get; set; }
        // 🎯 Thông tin điểm số
        public decimal? InstructorScore { get; set; }
        public decimal? PeerAverageScore { get; set; }
        public decimal? FinalScore { get; set; }

        // 🗒️ Thông tin feedback
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }


        // Navigation properties
        public List<SubmissionCriteriaFeedbackResponse> CriteriaFeedbacks { get; set; } = new();
        public AssignmentInfoResponse Assignment { get; set; }
        public UserInfoResponse User { get; set; }
        public List<SubmissionReviewAssignmentResponse> ReviewAssignments { get; set; }
        public List<AISummaryResponse> AISummaries { get; set; }
        public List<RegradeRequestSubmissionResponse> RegradeRequests { get; set; }
    }

    public class SubmissionCriteriaFeedbackResponse
    {
        public int CriteriaId { get; set; }
        public decimal? ScoreAwarded { get; set; }
        public string Feedback { get; set; }
    }

    public class AssignmentInfoResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public string CourseName { get; set; }
    }

    public class UserInfoResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string StudentId { get; set; }
    }

    public class SubmissionReviewAssignmentResponse
    {
        public int ReviewAssignmentId { get; set; }
        public int ReviewerId { get; set; }
        public string Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public UserInfoResponse Reviewer { get; set; }
    }


    public class RegradeRequestSubmissionResponse
    {
        public int RequestId { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}