using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.AISummary
{
    public class AICriteriaResponse
    {
        public List<AICriteriaFeedbackItem> Feedbacks { get; set; } = new List<AICriteriaFeedbackItem>();
    }

    public class AICriteriaFeedbackItem
    {
        public int CriteriaId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }  // Optional
        public string Summary { get; set; }  // Max 30 từ, concise
        public decimal Score { get; set; }  // 0 đến MaxScore
        public decimal MaxScore { get; set; }  // Từ rubric
    }
}