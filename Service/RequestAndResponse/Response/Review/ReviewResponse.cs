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
        public string DisplayScore { get; set; }
        public string GeneralFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewType { get; set; }
        public string FeedbackSource { get; set; }
        public bool IsAIReference { get; set; }
        public string DisplayNote { get; set; }
        public bool CanEdit { get; set; }
        public DateTime? EditDeadline { get; set; }
        public string EditStatus { get; set; }

        // Additional info from related entities
        public int SubmissionId { get; set; }

        // 🧍 Reviewer Info (THÊM DÒNG NÀY)
        public int? ReviewerId { get; set; }

        public string ReviewerName { get; set; }
        public string ReviewerEmail { get; set; }
        public string AssignmentTitle { get; set; }
        public string CourseName { get; set; }

        public List<CriteriaFeedbackResponse> CriteriaFeedbacks { get; set; } = new List<CriteriaFeedbackResponse>();

        // Method để set display score
        public void SetDisplayScore(string gradingScale)
        {
            if (!OverallScore.HasValue)
            {
                DisplayScore = "N/A";
                return;
            }

            if (gradingScale == "PassFail")
            {
                DisplayScore = OverallScore >= 50 ? "Pass" : "Fail";
            }
            else
            {
                DisplayScore = $"{OverallScore.Value:0.0}";
            }
        }
    }
}
