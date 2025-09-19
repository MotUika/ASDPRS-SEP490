using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int CourseInstanceId { get; set; }

        public int? RubricId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string Guidelines { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        [Required]
        public int NumPeerReviewsRequired { get; set; } = 0;

        [Required]
        public bool AllowCrossClass { get; set; } = false;

        [Required]
        public bool IsBlindReview { get; set; } = false;

        [Required]
        public decimal InstructorWeight { get; set; } = 0;

        [Required]
        public decimal PeerWeight { get; set; } = 0;

        [Required]
        public bool IncludeAIScore { get; set; } = false;

        [ForeignKey(nameof(CourseInstanceId))]
        public virtual CourseInstance CourseInstance { get; set; }

        [ForeignKey(nameof(RubricId))]
        public virtual Rubric Rubric { get; set; }

        // Navigation properties
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
