using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.User
{
    public class UserDetailResponse : UserResponse
    {
        public List<EnrolledCourseDetail> EnrolledCourses { get; set; } = new List<EnrolledCourseDetail>();
        public List<TaughtCourseDetail> TaughtCourses { get; set; } = new List<TaughtCourseDetail>();
        public List<SubmissionHistory> SubmissionsHistory { get; set; } = new List<SubmissionHistory>();
    }

    public class EnrolledCourseDetail
    {
        public int CourseInstanceId { get; set; }
        public string CourseName { get; set; }
        public string Status { get; set; }
        public decimal? FinalGrade { get; set; }
        public bool IsPassed { get; set; }
    }

    public class TaughtCourseDetail
    {
        public int CourseInstanceId { get; set; }
        public string CourseName { get; set; }
    }

    public class SubmissionHistory
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int CourseInstanceId { get; set; }
        public string CourseName { get; set; }
        public string SemesterName { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; }
        public decimal? FinalScore { get; set; }
    }
}
