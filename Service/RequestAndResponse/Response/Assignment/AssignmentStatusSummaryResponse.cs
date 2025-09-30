using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentStatusSummaryResponse
    {
        public int TotalAssignments { get; set; }
        public int DraftCount { get; set; }
        public int ScheduledCount { get; set; }
        public int ActiveCount { get; set; }
        public int LateSubmissionCount { get; set; }
        public int ClosedCount { get; set; }
        public int ArchivedCount { get; set; }
    }
}
