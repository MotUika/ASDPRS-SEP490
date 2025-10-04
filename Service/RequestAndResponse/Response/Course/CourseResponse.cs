namespace Service.RequestAndResponse.Response.Course
{
    public class CourseResponse
    {
        public int CourseId { get; set; }
        public int CurriculumId { get; set; }
        public string CurriculumName { get; set; }
        public string MajorName { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Credits { get; set; }
        public bool IsActive { get; set; }
        public int CourseInstanceCount { get; set; }
    }
}