namespace Service.RequestAndResponse.Response.AcademicYear
{
    public class AcademicYearResponse
    {
        public int AcademicYearId { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterCount { get; set; }
    }
}