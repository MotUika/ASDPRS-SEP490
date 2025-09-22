namespace Service.RequestAndResponse.Response.CriteriaFeedback
{
    public class CriteriaFeedbackResponse
    {
        public int CriteriaFeedbackId { get; set; }
        public int ReviewId { get; set; }
        public int CriteriaId { get; set; }
        public string CriteriaTitle { get; set; }
        public decimal? ScoreAwarded { get; set; }
        public string Feedback { get; set; }
        public string FeedbackSource { get; set; }
    }
}