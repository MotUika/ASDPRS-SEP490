using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Dashboard
{
    public class SemesterStatisticResponse
    {
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }

        public int TotalActiveAssignments { get; set; }
        public int TotalInReviewAssignments { get; set; }
        public int TotalClosedAssignments { get; set; }

        public int TotalRubricsUsed { get; set; }
        public int TotalCriteriaUsed { get; set; }

        public List<LowSubmissionAssignmentResponse> LowestSubmissionAssignments { get; set; } = new List<LowSubmissionAssignmentResponse>();

        public SubmissionRateResponse SubmissionRate { get; set; }

        public List<ScoreDistributionResponse> ScoreDistribution { get; set; } = new List<ScoreDistributionResponse>();
    }

    public class LowSubmissionAssignmentResponse
    {
        public string AssignmentTitle { get; set; }
        public string CourseName { get; set; }
        public string ClassName { get; set; }
        public decimal SubmissionRate { get; set; }
    }

    public class SubmissionRateResponse
    {
        public StatisticItem NotSubmitted { get; set; }
        public StatisticItem Submitted { get; set; }
        public StatisticItem Graded { get; set; }
    }

    public class ScoreDistributionResponse
    {
        public string RangeLabel { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class StatisticItem
    {
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}

