using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class CreateAssignmentRequest
    {
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

        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        [Required]
        [Range(0, 10)]
        public int NumPeerReviewsRequired { get; set; } = 0;

        [Required]
        public bool AllowCrossClass { get; set; } = false;

        [Required]
        public bool IsBlindReview { get; set; } = false;

        [Required]
        [Range(0, 100)]
        public decimal InstructorWeight { get; set; } = 0;

        [Required]
        [Range(0, 100)]
        public decimal PeerWeight { get; set; } = 0;

        [Required]
        public bool IncludeAIScore { get; set; } = false;
    }
}