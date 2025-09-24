using System;

namespace Service.RequestAndResponse.Response.CourseInstructor
{
    public class CourseInstructorResponse
    {
        public int Id { get; set; }
        public int CourseInstanceId { get; set; }
        public string CourseInstanceName { get; set; }
        public string CourseCode { get; set; }
        public int UserId { get; set; }
        public string InstructorName { get; set; }
        public string InstructorEmail { get; set; }
        public bool IsMainInstructor { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}