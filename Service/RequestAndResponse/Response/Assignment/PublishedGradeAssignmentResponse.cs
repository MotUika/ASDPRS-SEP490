using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class PublishedGradeAssignmentResponse
    {
        public int AssignmentId { get; set; }
        public int CourseInstanceId { get; set; }
        public int? SubmissionId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;

        public DateTime Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }

        public decimal FinalScore { get; set; }
        public string FormattedScore { get; set; } = string.Empty;
        public string GradingScale { get; set; } = "Scale10";

        public DateTime PublishedAt { get; set; }
    }
}
