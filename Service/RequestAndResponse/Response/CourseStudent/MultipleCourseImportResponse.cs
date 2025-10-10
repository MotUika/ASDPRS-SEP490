namespace Service.RequestAndResponse.Response.CourseStudent
{
    public class MultipleCourseImportResponse
    {
        public int TotalSuccessCount { get; set; }
        public List<SheetImportResult> SheetResults { get; set; }
    }

    public class SheetImportResult
    {
        public string SheetName { get; set; }
        public int? CourseInstanceId { get; set; }
        public string CourseName { get; set; }
        public int SuccessCount { get; set; }
        public string Message { get; set; }
        public List<CourseStudentResponse> ImportedStudents { get; set; }
    }
}