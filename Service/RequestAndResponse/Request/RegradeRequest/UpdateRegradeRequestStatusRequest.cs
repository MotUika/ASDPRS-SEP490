using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class UpdateRegradeRequestStatusRequest
    {
        [Required(ErrorMessage = "RequestId is required")]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        [StringLength(500, ErrorMessage = "ResolutionNotes cannot exceed 500 characters")]
        public string ResolutionNotes { get; set; }

        [Required(ErrorMessage = "ReviewedByInstructorId is required")]
        public int ReviewedByInstructorId { get; set; }
    }
}