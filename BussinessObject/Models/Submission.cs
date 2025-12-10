using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class Submission
    {
        [Key]
        public int SubmissionId { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string FileUrl { get; set; }

        [Required]
        public string FileName { get; set; }

        public string OriginalFileName { get; set; }

        public string Keywords { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        [Required]
        public string Status { get; set; }

        public bool IsPublic { get; set; }

        [ForeignKey("AssignmentId")]
        public Assignment Assignment { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public decimal? InstructorScore { get; set; }
        public decimal? PeerAverageScore { get; set; }
        public decimal? OldScore { get; set; }
        public decimal? FinalScore { get; set; }
        [StringLength(1000)]
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }


        public ICollection<ReviewAssignment> ReviewAssignments { get; set; }
        public ICollection<AISummary> AISummaries { get; set; }
        public ICollection<RegradeRequest> RegradeRequests { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}