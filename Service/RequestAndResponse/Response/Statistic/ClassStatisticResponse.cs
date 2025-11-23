using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class ClassStatisticResponse
    {
        public int CourseInstanceId { get; set; }
        public string SectionCode { get; set; }

        // Số lượng cơ bản
        public int TotalStudents { get; set; }
        public int TotalAssignments { get; set; }

        // Bài nộp / bài chấm
        public int ExpectedSubmissions { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedCount { get; set; }

        // Tỷ lệ
        public decimal SubmissionRate { get; set; }
        public decimal GradedRate { get; set; }

        // Điểm số
        public decimal? AverageScore { get; set; }
        public decimal? MinScore { get; set; }
        public decimal? MaxScore { get; set; }

        // Đậu / rớt
        public int PassCount { get; set; }
        public int FailCount { get; set; }

        public int FailStudentCount { get; set; }          // số sinh viên rớt ít nhất 1 bài
        public int PassAllAssignmentsCount { get; set; }   // số sinh viên pass tất cả bài
        public int FailAllAssignmentsCount { get; set; }

        // Phân phối điểm
        public List<DistributionItem> Distribution { get; set; } = new();

        // Thống kê chi tiết thêm
        public List<StudentAverageScoreResponse> StudentAverages { get; set; } = new();
        public List<AssignmentAverageScoreResponse> AssignmentAverages { get; set; } = new();
        public List<NormalizedScoreResponse> NormalizedScores { get; set; } = new();
    }


    public class StudentAverageScoreResponse
    {
        public int UserId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public decimal? AverageScore { get; set; }
        public List<StudentAssignmentScore> AssignmentScores { get; set; } = new();
    }

    public class StudentAssignmentScore
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public decimal? Score { get; set; }
    }

    public class AssignmentAverageScoreResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public decimal? AverageScore { get; set; }
    }

    public class NormalizedScoreResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public decimal? AverageNormalizedScore { get; set; }
    }

}