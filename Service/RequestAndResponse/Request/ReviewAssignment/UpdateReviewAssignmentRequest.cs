using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.ReviewAssignment
{
    public class UpdateReviewAssignmentRequest
    {
        [Required]
        public int ReviewAssignmentId { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        public DateTime? Deadline { get; set; }

        public bool? IsAIReview { get; set; }
    }
}