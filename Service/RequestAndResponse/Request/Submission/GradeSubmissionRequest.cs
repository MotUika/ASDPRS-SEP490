using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class GradeSubmissionRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "InstructorId is required")]
        public int InstructorId { get; set; }

        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        public decimal InstructorScore { get; set; }

        [StringLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
        public string Feedback { get; set; }

        // Tùy chọn: nếu bạn muốn update trạng thái submission -> “Graded”
        public bool PublishImmediately { get; set; } = false;
    }
}
