using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentStatsResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int TotalSubmissions { get; set; }
        public int TotalReviews { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal SubmissionRate { get; set; } // Percentage
        public decimal ReviewCompletionRate { get; set; } // Percentage
        public Dictionary<string, int> GradeDistribution { get; set; } = new Dictionary<string, int>();
    }
}