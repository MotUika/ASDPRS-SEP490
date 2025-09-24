using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class CreateRegradeRequestRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string Reason { get; set; }

        [Required(ErrorMessage = "RequestedByUserId is required")]
        public int RequestedByUserId { get; set; }
    }
}