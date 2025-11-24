using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class UpdateAssignmentRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        public int? RubricTemplateId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string Guidelines { get; set; }

        public IFormFile? File { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? Deadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        public DateTime? FinalDeadline { get; set; }

        [Range(0, 10)]
        public int? NumPeerReviewsRequired { get; set; }
        public decimal? PassThreshold { get; set; } = 50;

        public bool? AllowCrossClass { get; set; }
        public string? CrossClassTag { get; set; }

        //[JsonIgnore]
        //public bool IsBlindReview { get; set; } = true;
        //[JsonIgnore]
        //public bool IncludeAIScore { get; set; } = false;

        [Range(0, 100)]
        public decimal? InstructorWeight { get; set; }

        [Range(0, 100)]
        public decimal? PeerWeight { get; set; }
        public string? GradingScale { get; set; }

        [Range(0, 10)]
        public decimal? MissingReviewPenalty { get; set; } = 0;
    }
}