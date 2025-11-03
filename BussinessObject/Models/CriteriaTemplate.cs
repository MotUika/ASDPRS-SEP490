using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class CriteriaTemplate
    {
        [Key]
        public int CriteriaTemplateId { get; set; }

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

        //[Required]
        [StringLength(50)]
        public string ScoringType { get; set; }

        [StringLength(50)]
        public string ScoreLabel { get; set; }

        [ForeignKey(nameof(TemplateId))]
        public virtual RubricTemplate Template { get; set; }

        // Navigation properties
        public virtual ICollection<Criteria> Criteria { get; set; } = new List<Criteria>();
    }
}
