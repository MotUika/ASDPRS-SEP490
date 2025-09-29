using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentBasicResponse
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
    }
}
