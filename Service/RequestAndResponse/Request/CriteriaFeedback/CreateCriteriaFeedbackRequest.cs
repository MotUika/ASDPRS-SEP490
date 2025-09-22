using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CriteriaFeedback
{
    public class CreateCriteriaFeedbackRequest
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        public int CriteriaId { get; set; }

        public decimal? ScoreAwarded { get; set; }

        [StringLength(500)]
        public string Feedback { get; set; }

        [StringLength(50)]
        public string FeedbackSource { get; set; }
    }
}