using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class Curriculum
    {
        [Key]
        public int CurriculumId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        public int MajorId { get; set; }

        [Required]
        [StringLength(100)]
        public string CurriculumName { get; set; }

        [Required]
        [StringLength(20)]
        public string CurriculumCode { get; set; }

        public int TotalCredits { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(CampusId))]
        public virtual Campus Campus { get; set; }

        [ForeignKey(nameof(MajorId))]
        public virtual Major Major { get; set; }

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}