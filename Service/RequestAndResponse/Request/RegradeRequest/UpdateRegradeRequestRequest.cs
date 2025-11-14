using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class UpdateRegradeRequestRequest
    {
        [Required(ErrorMessage = "RequestId is required")]
        public int RequestId { get; set; }

        [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string Reason { get; set; }

        public string Status { get; set; }

        public string ResolutionNotes { get; set; }

        public int? ReviewedByInstructorId { get; set; }

        public int? ReviewedByUserId { get; set; }
    }
}