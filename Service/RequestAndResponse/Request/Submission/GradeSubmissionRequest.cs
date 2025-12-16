using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Submission
{
    public class SubmissionCriteriaFeedbackRequest
    {
        public int CriteriaId { get; set; }
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
    }


    public class GradeSubmissionRequest
    {
        [Required(ErrorMessage = "SubmissionId is required")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "InstructorId is required")]
        public int InstructorId { get; set; }

        [StringLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
        public string? Feedback { get; set; }

        public bool PublishImmediately { get; set; } = false;

        // Thêm phần rubric criteria
        public List<SubmissionCriteriaFeedbackRequest>? CriteriaFeedbacks { get; set; }
    }
}
