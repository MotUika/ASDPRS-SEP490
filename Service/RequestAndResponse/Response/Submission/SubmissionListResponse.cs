using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Submission
{
    public class SubmissionListResponse
    {
        public List<SubmissionResponse> Submissions { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalStudents { get; set; }
    }
}