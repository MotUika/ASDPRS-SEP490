using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class CreateAISummaryRequest
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        [Required]
        [StringLength(50)]
        public string SummaryType { get; set; }
    }
}