using System.Collections.Generic;


namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentOverviewResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        public int TotalStudents { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedCount { get; set; }

        public decimal SubmissionRate { get; set; }
        public decimal GradedRate { get; set; }

        public int PassCount { get; set; }
        public int FailCount { get; set; }
    }
}
