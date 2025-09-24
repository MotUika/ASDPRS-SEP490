using System;
using System.Collections.Generic;
using Service.RequestAndResponse.Response.Review;

namespace Service.RequestAndResponse.Response.ReviewAssignment
{
    public class ReviewAssignmentResponse
    {
        public int ReviewAssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public int ReviewerUserId { get; set; }
        public string Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime Deadline { get; set; }
        public bool IsAIReview { get; set; }

        // Additional info
        public string ReviewerName { get; set; }
        public string ReviewerEmail { get; set; }
        public string AssignmentTitle { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string CourseName { get; set; }

        public List<ReviewResponse> Reviews { get; set; } = new List<ReviewResponse>();
        public bool IsOverdue => DateTime.UtcNow > Deadline && Status != "Completed";
        public int DaysUntilDeadline => (int)(Deadline - DateTime.UtcNow).TotalDays;
    }
}