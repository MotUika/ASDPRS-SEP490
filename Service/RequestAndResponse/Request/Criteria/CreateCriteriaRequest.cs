using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Criteria
{
    public class CreateCriteriaRequest
    {
        [Required]
        public int RubricId { get; set; }

        public int? CriteriaTemplateId { get; set; }

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

        [Required]
        public bool IsModified { get; set; } = false;
    }
}