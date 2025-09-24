using System;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentSummaryResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }
        public int SubmissionCount { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDeadline { get; set; }
        public string Status { get; set; }
    }
}