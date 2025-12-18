using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Submission
{
    public class SubmissionDetailExportResponse
    {
        // Student info
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string StudentCode { get; set; }

        // Assignment info
        public int AssignmentId { get; set; }
        public string AssignmentName { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }

        // Submission info
        public int SubmissionId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public decimal? FinalScore { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string Feedback { get; set; }

        // Criteria details
        public List<SubmissionCriteriaScoreExport> CriteriaScores { get; set; }
    }

    public class SubmissionCriteriaScoreExport
    {
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; }
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
        public decimal Weight { get; set; }
    }

}
