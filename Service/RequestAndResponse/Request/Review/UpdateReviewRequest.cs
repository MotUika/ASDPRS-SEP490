using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Review
{
    public class UpdateReviewRequest
    {
        [Required]
        public int ReviewId { get; set; }

        public decimal? OverallScore { get; set; }

        [StringLength(1000)]
        public string GeneralFeedback { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(50)]
        public string ReviewType { get; set; }

        [StringLength(50)]
        public string FeedbackSource { get; set; }

        public List<CriteriaFeedbackRequest> CriteriaFeedbacks { get; set; }
    }
}