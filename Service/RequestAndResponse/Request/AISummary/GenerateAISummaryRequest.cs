using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class GenerateAISummaryRequest
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [StringLength(50)]
        public string SummaryType { get; set; }

        [StringLength(1000)]
        public string AdditionalInstructions { get; set; }

        public bool ForceRegenerate { get; set; } = false;
    }
}