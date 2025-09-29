using Service.RequestAndResponse.Response.Rubric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.ReviewAssignment
{
    public class ReviewAssignmentDetailResponse
    {
        public int ReviewAssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public int AssignmentId { get; set; }
        public string Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string AssignmentTitle { get; set; }
        public string StudentName { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public RubricResponse Rubric { get; set; }
    }
}
