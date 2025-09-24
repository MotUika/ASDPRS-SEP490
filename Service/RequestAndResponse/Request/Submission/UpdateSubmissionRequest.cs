using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class UpdateSubmissionRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        public IFormFile File { get; set; }

        [StringLength(500, ErrorMessage = "Keywords cannot exceed 500 characters")]
        public string Keywords { get; set; }

        public bool? IsPublic { get; set; }

        public string Status { get; set; }
    }
}