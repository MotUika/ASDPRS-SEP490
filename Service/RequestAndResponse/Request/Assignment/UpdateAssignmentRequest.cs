using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class UpdateAssignmentRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        public int? RubricId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string Guidelines { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        public DateTime? FinalDeadline { get; set; }

        [Range(0, 10)]
        public int? NumPeerReviewsRequired { get; set; }

        public bool? AllowCrossClass { get; set; }

        public bool? IsBlindReview { get; set; }

        [Range(0, 100)]
        public decimal? InstructorWeight { get; set; }

        [Range(0, 100)]
        public decimal? PeerWeight { get; set; }
        public string? GradingScale { get; set; }
        public decimal? Weight { get; set; }
        public decimal? LateSubmissionPenalty { get; set; }
        public decimal? MissingReviewPenalty { get; set; }
        public bool? IncludeAIScore { get; set; }
    }
}