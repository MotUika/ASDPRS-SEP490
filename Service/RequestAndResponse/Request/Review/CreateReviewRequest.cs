using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Review
{
    public class CreateReviewRequest
    {
        [Required]
        public int ReviewAssignmentId { get; set; }

        public decimal? OverallScore { get; set; }

        [StringLength(1000)]
        public string GeneralFeedback { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [Required]
        [StringLength(50)]
        public string ReviewType { get; set; }

        [StringLength(50)]
        public string FeedbackSource { get; set; }

        public List<CriteriaFeedbackRequest> CriteriaFeedbacks { get; set; } = new List<CriteriaFeedbackRequest>();
    }

    public class CriteriaFeedbackRequest
    {
        [Required]
        public int CriteriaId { get; set; }

        public decimal? Score { get; set; }

        [StringLength(500)]
        public string Feedback { get; set; }
    }
}