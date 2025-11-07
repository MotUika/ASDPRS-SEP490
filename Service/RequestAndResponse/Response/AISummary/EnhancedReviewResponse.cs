using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.AISummary
{
    public class EnhancedReviewResponse
    {
        public int SummaryId { get; set; }
        public string Content { get; set; }
        public string SummaryType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool WasGenerated { get; set; }
        public string Status { get; set; }

        // Thêm danh sách criteria reviews
        public List<CriteriaReviewResponse> CriteriaReviews { get; set; } = new List<CriteriaReviewResponse>();
    }

    public class CriteriaReviewResponse
    {
        public int SummaryId { get; set; }
        public string Content { get; set; }
        public string CriteriaTitle { get; set; }
        public string CriteriaDescription { get; set; }
        public int CriteriaWeight { get; set; }
        public decimal CriteriaMaxScore { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
