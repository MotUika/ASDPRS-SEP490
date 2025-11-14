using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class CreateAssignmentRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        public int? RubricTemplateId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string Guidelines { get; set; }

        // File upload
        public IFormFile? File { get; set; }

        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        public DateTime? FinalDeadline { get; set; }

        [Required]
        [Range(0, 10)]
        public int NumPeerReviewsRequired { get; set; } = 0;

        public decimal? PassThreshold { get; set; } = 50;

        [Range(0, 10)]
        public decimal? MissingReviewPenalty { get; set; } = 0;

        [Required]
        public bool AllowCrossClass { get; set; } = false;

        //public bool IsBlindReview { get; set; } = true;

        //public bool IncludeAIScore { get; set; } = false;


        [Required]
        [Range(0, 100)]
        public decimal InstructorWeight { get; set; } = 0;

        [Required]
        [Range(0, 100)]
        public decimal PeerWeight { get; set; } = 0;
        public string GradingScale { get; set; } = "Scale10";

    }
}