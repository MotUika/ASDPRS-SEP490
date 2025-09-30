using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Assignment
{
    public class UpdateAssignmentTimelineRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? FinalDeadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
    }
}
