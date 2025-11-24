using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentSubmissionDetailResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        public List<SubmissionStatisticResponse> Submissions { get; set; } = new List<SubmissionStatisticResponse>();

        public int SubmittedCount { get; set; }  // tất cả bài đã nộp
        public int NotSubmittedCount { get; set; } // chưa nộp
        public int GradedCount { get; set; } // đã chấm
    }

    public class SubmissionStatisticResponse
    {
        public int SubmissionId { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public decimal? FinalScore { get; set; }
        public string Status { get; set; }  // Not Submitted / Submitted / Graded
    }
}
