namespace Service.RequestAndResponse.Response.Curriculum
{
    public class CurriculumResponse
    {
        public int CurriculumId { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public int MajorId { get; set; }
        public string MajorName { get; set; }
        public string CurriculumName { get; set; }
        public string CurriculumCode { get; set; }
        public int TotalCredits { get; set; }
        public bool IsActive { get; set; }
        public int CourseCount { get; set; }
    }
}