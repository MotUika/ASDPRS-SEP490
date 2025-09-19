using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Rubric
    {
        [Key]
        public int RubricId { get; set; }

        public int? TemplateId { get; set; }

        public int? AssignmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public bool IsModified { get; set; } = false;

        [ForeignKey(nameof(TemplateId))]
        public virtual RubricTemplate Template { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public virtual Assignment Assignment { get; set; }

        // Navigation properties
        public virtual ICollection<Criteria> Criteria { get; set; } = new List<Criteria>();
    }
}
