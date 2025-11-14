namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class GetRegradeRequestsByFilterRequest
    {
        public int? SubmissionId { get; set; }
        public int? StudentId { get; set; }
        public int? InstructorId { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; }
        public int? AssignmentId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}