using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class CourseInstance
    {
        [Key]
        public int CourseInstanceId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(20)]
        public string SectionCode { get; set; }

        [StringLength(50)]
        public string EnrollmentPassword { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;


        // REMOVED: MaxStudents field
        [Required]
        public bool RequiresApproval { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual Semester Semester { get; set; }

        [ForeignKey(nameof(CampusId))]
        public virtual Campus Campus { get; set; }

        // Navigation properties
        public virtual ICollection<CourseInstructor> CourseInstructors { get; set; } = new List<CourseInstructor>();
        public virtual ICollection<CourseStudent> CourseStudents { get; set; } = new List<CourseStudent>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
