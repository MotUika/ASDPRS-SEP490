using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class UpdateSubmissionStatusRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        public string Notes { get; set; }
    }
}