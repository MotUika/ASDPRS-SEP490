using Service.RequestAndResponse.Response.CriteriaFeedback;
using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Review
{
    public class ReviewResponse
    {
        public int ReviewId { get; set; }
        public int ReviewAssignmentId { get; set; }
        public decimal? OverallScore { get; set; }
        public string GeneralFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewType { get; set; }
        public string FeedbackSource { get; set; }

        // Additional info from related entities
        public int SubmissionId { get; set; }
        public string ReviewerName { get; set; }
        public string ReviewerEmail { get; set; }
        public string AssignmentTitle { get; set; }
        public string CourseName { get; set; }

        public List<CriteriaFeedbackResponse> CriteriaFeedbacks { get; set; } = new List<CriteriaFeedbackResponse>();
    }
}