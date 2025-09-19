using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Criteria
    {
        [Key]
        public int CriteriaId { get; set; }

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

        [ForeignKey(nameof(RubricId))]
        public virtual Rubric Rubric { get; set; }

        [ForeignKey(nameof(CriteriaTemplateId))]
        public virtual CriteriaTemplate CriteriaTemplate { get; set; }

        // Navigation properties
        public virtual ICollection<CriteriaFeedback> CriteriaFeedbacks { get; set; } = new List<CriteriaFeedback>();
    }
}
