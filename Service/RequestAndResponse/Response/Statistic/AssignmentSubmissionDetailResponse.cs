using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentSubmissionDetailResponse
    {
        // --- Tổng hợp tất cả assignment ---
        public int TotalAssignment { get; set; }
        public int TotalSubmittedCount { get; set; }
        public int TotalNotSubmittedCount { get; set; }
        public int TotalGradedCount { get; set; }

        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        public List<SubmissionStatisticResponse> Submissions { get; set; } = new List<SubmissionStatisticResponse>();
        public int SubmittedCount { get; set; }
        public int NotSubmittedCount { get; set; }
        public int GradedCount { get; set; }
    }

    public class SubmissionStatisticResponse
    {
        public int SubmissionId { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public decimal? FinalScore { get; set; }
        public string Status { get; set; }                  // Not Submitted / Submitted / Graded
    }
}