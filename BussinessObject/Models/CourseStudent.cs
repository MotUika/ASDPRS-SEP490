using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class CourseStudent
    {
        [Key]
        public int CourseStudentId { get; set; }

        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime EnrolledAt { get; set; }

        public string Status { get; set; }

        public decimal? FinalGrade { get; set; }

        public bool IsPassed { get; set; }

        public DateTime? StatusChangedAt { get; set; }

        public int? ChangedByUserId { get; set; }

        [ForeignKey("CourseInstanceId")]
        public CourseInstance CourseInstance { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("ChangedByUserId")]
        public User ChangedByUser { get; set; }
    }
}