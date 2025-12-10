namespace Service.RequestAndResponse.Response.Course
{
    public class CourseResponse
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public bool IsActive { get; set; }
        public int CourseInstanceCount { get; set; }
    }
}