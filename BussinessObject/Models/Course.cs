using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        public int CurriculumId { get; set; }

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int Credits { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(CurriculumId))]
        public virtual Curriculum Curriculum { get; set; }

        // Navigation properties
        public virtual ICollection<CourseInstance> CourseInstances { get; set; } = new List<CourseInstance>();
    }
}
