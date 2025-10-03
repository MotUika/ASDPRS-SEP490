using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class CloneAssignmentRequest
    {
        public string NewTitle { get; set; }
        public DateTime? NewStartDate { get; set; }
        public DateTime? NewDeadline { get; set; }
        public DateTime? NewFinalDeadline { get; set; }
        public DateTime? NewReviewDeadline { get; set; }
        public decimal? LateSubmissionPenalty { get; set; }
        public decimal? MissingReviewPenalty { get; set; }
    }
}
