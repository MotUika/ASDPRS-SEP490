using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class StudentSemesterScoreResponse
    {
        public int CourseInstanceId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }

        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public DateTime Deadline { get; set; }

        public decimal? FinalScore { get; set; }
        public string FormattedScore { get; set; }
        public string GradeStatus { get; set; }
        public string GradingScale { get; set; }
    }
}
