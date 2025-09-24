using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class CreateSubmissionRequest
    {
        [Required(ErrorMessage = "AssignmentId is required")]
        public int AssignmentId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }

        [StringLength(500, ErrorMessage = "Keywords cannot exceed 500 characters")]
        public string Keywords { get; set; }

        public bool IsPublic { get; set; } = false;
    }
}