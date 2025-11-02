using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class RegradeRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Reason { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        public int? ReviewedByInstructorId { get; set; }

        public string? ResolutionNotes { get; set; }

        [ForeignKey("SubmissionId")]
        public Submission Submission { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public User ReviewedByUser { get; set; }

        [ForeignKey("ReviewedByInstructorId")]
        public User ReviewedByInstructor { get; set; }
    }
}