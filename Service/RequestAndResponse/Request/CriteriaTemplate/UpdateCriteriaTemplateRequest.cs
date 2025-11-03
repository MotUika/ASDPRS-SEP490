using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CriteriaTemplate
{
    public class UpdateCriteriaTemplateRequest
    {
        [Required]
        public int CriteriaTemplateId { get; set; }

        public int TemplateId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int Weight { get; set; }

        public decimal MaxScore { get; set; }

        //[StringLength(50)]
        //public string ScoringType { get; set; }

        //[StringLength(50)]
        //public string ScoreLabel { get; set; }
    }
}