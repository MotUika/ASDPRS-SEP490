using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class SubmissionStatisticsResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int TotalSubmissions { get; set; }
        public int PendingSubmissions { get; set; }
        public int GradedSubmissions { get; set; }
        public int LateSubmissions { get; set; }
        public double AverageScore { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; }
        public List<KeywordFrequencyResponse> TopKeywords { get; set; }
    }

    public class KeywordFrequencyResponse
    {
        public string Keyword { get; set; }
        public int Frequency { get; set; }
    }
}