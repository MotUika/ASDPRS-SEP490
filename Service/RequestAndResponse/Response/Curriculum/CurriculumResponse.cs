namespace Service.RequestAndResponse.Response.Curriculum
{
    public class CurriculumResponse
    {
        public int CurriculumId { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string MajorCode { get; set; }
        public string MajorName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int CourseCount { get; set; }
    }
}