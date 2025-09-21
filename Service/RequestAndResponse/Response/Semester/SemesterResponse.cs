namespace Service.RequestAndResponse.Response.Semester
{
    public class SemesterResponse
    {
        public int SemesterId { get; set; }
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int CourseInstanceCount { get; set; }
    }
}