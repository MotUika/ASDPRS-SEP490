using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.ReviewAssignment
{
    public class CreateReviewAssignmentRequest
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int ReviewerUserId { get; set; }

        [Required]
        public string Status { get; set; } = "Assigned";

        [Required]
        public DateTime Deadline { get; set; }

        public bool IsAIReview { get; set; } = false;
    }

    public class BulkCreateReviewAssignmentRequest
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public List<int> ReviewerUserIds { get; set; } = new List<int>();

        [Required]
        public DateTime Deadline { get; set; }

        public bool IsAIReview { get; set; } = false;
    }
}