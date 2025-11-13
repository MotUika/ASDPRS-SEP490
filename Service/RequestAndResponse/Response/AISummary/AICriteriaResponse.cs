using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.AISummary
{
        public class AICriteriaResponse
        {
            public List<AICriteriaFeedbackItem> Feedbacks { get; set; } = new List<AICriteriaFeedbackItem>();
            public bool IsRelevant { get; set; } = true;
            public string ErrorMessage { get; set; }
        }

        public class AICriteriaFeedbackItem
        {
            public int CriteriaId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Summary { get; set; }
            public decimal Score { get; set; }
            public decimal MaxScore { get; set; }
        }
    }