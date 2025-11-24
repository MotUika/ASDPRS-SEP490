using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Submission
{
    public class InstructorSubmissionInfoResponse
    {
        // User
        public int UserId { get; set; }
        public string Username { get; set; }
        public string StudentCode { get; set; }

        // Assignment
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        // Course & Class
        public int CourseInstanceId { get; set; }
        public int CourseId { get; set; }
        public string ClassName { get; set; }       // SectionCode
        public string CourseName { get; set; }

        // Submission
        public int SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string StatusSubmission { get; set; }
        public decimal? FinalScore { get; set; }
        public DateTime? GradedAt { get; set; }

        // Regrade Request (latest)
        public string RegradeReason { get; set; }
        public string RegradeStatus { get; set; }
        public DateTime? RequestedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string ResolutionNotes { get; set; }
    }

}
