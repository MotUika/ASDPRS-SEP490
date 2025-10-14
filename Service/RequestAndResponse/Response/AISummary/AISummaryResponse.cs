using System;

namespace Service.RequestAndResponse.Response.AISummary
{
    public class AISummaryResponse
    {
        public int SummaryId { get; set; }
        public int SubmissionId { get; set; }
        public string Content { get; set; }
        public string SummaryType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string AssignmentTitle { get; set; }
        public string CourseName { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string FileName { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}