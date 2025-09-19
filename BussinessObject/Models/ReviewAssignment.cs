using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class ReviewAssignment
    {
        [Key]
        public int ReviewAssignmentId { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int ReviewerUserId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        public bool IsAIReview { get; set; }

        [ForeignKey("SubmissionId")]
        public Submission Submission { get; set; }

        [ForeignKey("ReviewerUserId")]
        public User ReviewerUser { get; set; }

        public ICollection<Review> Reviews { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}