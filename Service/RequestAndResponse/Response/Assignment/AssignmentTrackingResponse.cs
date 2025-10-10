using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentTrackingResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Guidelines { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime ReviewDeadline { get; set; }
        public int NumPeerReviewsRequired { get; set; }
        public int PendingReviewsCount { get; set; }
        public int CompletedReviewsCount { get; set; }
        public bool HasMetMinimumReviews { get; set; }
        public int RemainingReviewsRequired { get; set; }
        public string ReviewStatus { get; set; } // "Completed", "In Progress", "Not Started"
        public decimal ReviewCompletionPercentage { get; set; }
    }
}
