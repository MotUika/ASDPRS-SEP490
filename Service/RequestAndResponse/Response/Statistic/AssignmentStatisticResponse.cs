using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentStatisticResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseInstanceId { get; set; }
        public string SectionCode { get; set; }

        public int TotalStudents { get; set; }           // tổng số sinh viên lớp
        public int TotalSubmissions { get; set; }        // số submission nộp
        public int GradedCount { get; set; }             // số submission đã chấm

        public decimal SubmissionRate { get; set; }      // tỉ lệ nộp bài (%)
        public decimal GradedRate { get; set; }          // tỉ lệ đã chấm (%)

        public decimal? AverageScore { get; set; }
        public decimal? MinScore { get; set; }
        public decimal? MaxScore { get; set; }

        public int PassCount { get; set; }
        public int FailCount { get; set; }

        // ✅ Thêm các field mới
        public int FailStudentCount { get; set; }          // số sinh viên rớt bài này
        public int PassAllAssignmentsCount { get; set; }   // số sinh viên pass bài này
        public int FailAllAssignmentsCount { get; set; }   // số sinh viên rớt bài này (giống FailStudentCount nhưng giữ tên thống nhất)

        public List<StudentAverageScoreResponse> StudentAverages { get; set; } = new();
        public List<DistributionItem> Distribution { get; set; } = new();
    }

    public class DistributionItem
    {
        public string Range { get; set; }
        public int Count { get; set; }
    }

    //public class StudentAssignmentScore
    //{
    //    public int AssignmentId { get; set; }
    //    public string AssignmentTitle { get; set; }
    //    public decimal? Score { get; set; }
    //}

    //public class StudentAverageScoreResponse
    //{
    //    public int UserId { get; set; }
    //    public string StudentName { get; set; }
    //    public string StudentCode { get; set; }
    //    public decimal? AverageScore { get; set; }
    //    public List<StudentAssignmentScore> AssignmentScores { get; set; } = new();
    //}


}
