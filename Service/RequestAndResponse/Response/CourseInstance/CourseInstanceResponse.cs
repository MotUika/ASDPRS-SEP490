namespace Service.RequestAndResponse.Response.CourseInstance
{
    public class CourseInstanceResponse
    {
        public int CourseInstanceId { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string SectionCode { get; set; }
        public string EnrollmentPassword { get; set; }
        public int MaxStudents { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime CreatedAt { get; set; }
        public int InstructorCount { get; set; }
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
    }
}