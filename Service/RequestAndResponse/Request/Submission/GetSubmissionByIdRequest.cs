using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class GetSubmissionByIdRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        public bool IncludeReviews { get; set; } = false;
        public bool IncludeAISummaries { get; set; } = false;
    }
}