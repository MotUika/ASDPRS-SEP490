using System;
using System.Text.Json.Serialization;

namespace Service.RequestAndResponse.Response.CourseStudent
{
    public class CourseStudentResponse
    {
        public int CourseStudentId { get; set; }
        public int CourseInstanceId { get; set; }
        public string CourseInstanceName { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string StudentCode { get; set; }
        public string Role { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string Status { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? FinalGrade { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsPassed { get; set; }
        public DateTime? StatusChangedAt { get; set; }
        public int? ChangedByUserId { get; set; }
        public string ChangedByUserName { get; set; }
        public int StudentCount { get; set; }
        public List<string> InstructorNames { get; set; } = new List<string>();
    }
}