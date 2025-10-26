using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class PublishGradesResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public bool IsPublished { get; set; }

        public int TotalStudents { get; set; }
        public int SubmittedCount { get; set; }
        public int GradedCount { get; set; }

        public double SubmissionRate { get; set; }
        public double GradedRate { get; set; }

        public string? Note { get; set; }
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public List<GradeStudentResult>? Results { get; set; }
    }

    public class GradeStudentResult
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public decimal? FinalScore { get; set; }
        public string? Feedback { get; set; }
    }
}
