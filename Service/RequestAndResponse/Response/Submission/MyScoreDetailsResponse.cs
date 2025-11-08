using System;

namespace Service.RequestAndResponse.Response.Submission
{
    public class MyScoreDetailsResponse
    {
        public int SubmissionId { get; set; }
        public decimal InstructorScore { get; set; }
        public decimal PeerAverageScore { get; set; }
        public decimal FinalScore { get; set; }
        public string Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public string RegradeStatus { get; set; }
        public decimal ClassAverageScore { get; set; }
        public decimal ClassMaxScore { get; set; }
        public string Note { get; set; }
    }
}