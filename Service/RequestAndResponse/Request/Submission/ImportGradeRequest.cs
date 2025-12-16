using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class ImportCriteriaScore
    {
        public int CriteriaId { get; set; }
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
    }

    public class ImportGradeRequest
    {
        public int SubmissionId { get; set; }
        public int InstructorId { get; set; }
        public string FinalFeedback { get; set; } // cột Feedback tổng
        public List<ImportCriteriaScore> CriteriaScores { get; set; } = new();
    }

}
