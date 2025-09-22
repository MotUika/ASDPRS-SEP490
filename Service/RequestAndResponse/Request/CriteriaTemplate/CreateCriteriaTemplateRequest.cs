using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CriteriaTemplate
{
    public class CreateCriteriaTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int Weight { get; set; }

        [Required]
        public decimal MaxScore { get; set; }

        [Required]
        [StringLength(50)]
        public string ScoringType { get; set; }

        [StringLength(50)]
        public string ScoreLabel { get; set; }
    }
}