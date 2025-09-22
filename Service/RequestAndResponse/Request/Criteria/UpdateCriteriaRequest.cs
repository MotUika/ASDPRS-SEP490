using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Criteria
{
    public class UpdateCriteriaRequest
    {
        [Required]
        public int CriteriaId { get; set; }

        public int RubricId { get; set; }

        public int? CriteriaTemplateId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int Weight { get; set; }

        public decimal MaxScore { get; set; }

        [StringLength(50)]
        public string ScoringType { get; set; }

        [StringLength(50)]
        public string ScoreLabel { get; set; }

        public bool IsModified { get; set; }
    }
}