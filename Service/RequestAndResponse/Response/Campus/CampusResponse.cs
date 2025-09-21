namespace Service.RequestAndResponse.Response.Campus
{
    public class CampusResponse
    {
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string Address { get; set; }
        public int UserCount { get; set; }
        public int AcademicYearCount { get; set; }
        public int CurriculumCount { get; set; }
        public int CourseInstanceCount { get; set; }
    }
}