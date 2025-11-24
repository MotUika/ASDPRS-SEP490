using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentOverviewResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        public int TotalStudents { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedCount { get; set; }

        public decimal SubmissionRate { get; set; }
        public decimal GradedRate { get; set; }

        public int PassCount { get; set; }
        public int FailCount { get; set; }

        // --- Thêm các field mới để đếm theo status ---
        public int DraftCount { get; set; }
        public int UpcomingCount { get; set; }
        public int ActiveCount { get; set; }
        public int InReviewCount { get; set; }
        public int ClosedCount { get; set; }
        public int GradesPublishedCount { get; set; }
        public int ArchivedCount { get; set; }
        public int CancelledCount { get; set; }
    }
}
