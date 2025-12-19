using System;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentSummaryResponse
    {
        public int AssignmentId { get; set; }
        public int CourseInstanceId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public DateTime? FinalDeadline { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }
        public int SubmissionCount { get; set; }
        public int StudentCount { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDeadline { get; set; }
        public string Status { get; set; }
        // Trạng thái phụ để hiển thị UI (Due Soon, Overdue, v.v.)
        public string UiStatus { get; set; }
    }
}