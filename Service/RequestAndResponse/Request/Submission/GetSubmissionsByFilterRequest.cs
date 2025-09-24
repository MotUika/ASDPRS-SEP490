namespace Service.RequestAndResponse.Request.Submission
{
    public class GetSubmissionsByFilterRequest
    {
        public int? AssignmentId { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; }
        public string Keywords { get; set; }
        public bool? IsPublic { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "SubmittedAt";
        public bool SortDescending { get; set; } = true;
    }
}