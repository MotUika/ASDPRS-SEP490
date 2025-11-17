using System;

namespace Service.RequestAndResponse.Response.Submission
{
    public class GradeSubmissionResponse
    {
        public int SubmissionId { get; set; }
        public int AssignmentId { get; set; }
        public int UserId { get; set; }

        // 🎯 Thông tin điểm số
        public decimal? InstructorScore { get; set; }
        public decimal? PeerAverageScore { get; set; }
        public decimal? FinalScore { get; set; }

        // ⭐ NEW: Điểm trước khi trừ penalty
        public decimal? FinalScoreBeforePenalty { get; set; }

        // ⭐ NEW: Thông tin Missing Review Penalty
        public int MissingReviews { get; set; }
        public decimal MissingReviewPenaltyPerReview { get; set; }
        public decimal MissingReviewPenaltyTotal { get; set; }

        // 🗒️ Thông tin feedback
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }

        // 📎 File bài nộp
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? OriginalFileName { get; set; }

        // 📘 Trạng thái bài nộp
        public string? Status { get; set; }
        public string? RegradeRequestStatus { get; set; }
        public bool IsPublic { get; set; }

        // 👨‍🎓 Thông tin sinh viên & bài tập
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string? CourseName { get; set; }
        public string? AssignmentTitle { get; set; }
    }
}
