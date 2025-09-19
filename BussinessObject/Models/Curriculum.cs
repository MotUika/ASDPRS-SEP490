using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Curriculum
    {
        [Key]
        public int CurriculumId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(20)]
        public string MajorCode { get; set; }

        [Required]
        [StringLength(100)]
        public string MajorName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(CampusId))]
        public virtual Campus Campus { get; set; }

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
