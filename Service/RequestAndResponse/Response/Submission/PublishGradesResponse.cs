// File: Service/RequestAndResponse/Response/Submission/PublishGradesResponse.cs
using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class PublishGradesResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public bool IsPublished { get; set; }

        // Thống kê
        public int TotalStudents { get; set; }
        public int SubmittedCount { get; set; }
        public int NotSubmittedCount { get; set; }
        public int GradedCount { get; set; }
        public int UngradedCount { get; set; }

        // Tỷ lệ (dùng decimal để chính xác)
        public decimal SubmissionRate => TotalStudents > 0 ? Math.Round((decimal)SubmittedCount / TotalStudents * 100, 2) : 0;
        public decimal GradedRate => SubmittedCount > 0 ? Math.Round((decimal)GradedCount / SubmittedCount * 100, 2) : 0;

        // Thông báo
        public string? Note { get; set; }
        public List<string> BlockingReasons { get; set; } = new();

        // Thời gian
        public DateTime? PublishedAt { get; set; } // Chỉ có khi IsPublished = true

        // Danh sách sinh viên (chỉ có khi public thành công)
        public List<GradeStudentResult> Results { get; set; } = new();
    }

    public class GradeStudentResult
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public decimal? FinalScore { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = string.Empty; // Graded, Not Submitted, etc.
    }
}