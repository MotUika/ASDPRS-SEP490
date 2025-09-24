using System.Collections.Generic;
using System.Linq;

namespace Service.RequestAndResponse.Response.ReviewAssignment
{
    public class PeerReviewStatsResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int TotalSubmissions { get; set; }
        public int RequiredReviewsPerSubmission { get; set; }
        public int CurrentStudentCount { get; set; }
        public int PassedStudentCount { get; set; }
        public double AverageReviewsPerSubmission { get; set; }
        public int CompletedSubmissions { get; set; }
        public List<SubmissionReviewStats> SubmissionStats { get; set; } = new List<SubmissionReviewStats>();
    }

    public class SubmissionReviewStats
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; }
        public int TotalReviews { get; set; }
        public int CurrentStudentReviews { get; set; }
        public int PassedStudentReviews { get; set; }
        public string Status { get; set; }
    }
}