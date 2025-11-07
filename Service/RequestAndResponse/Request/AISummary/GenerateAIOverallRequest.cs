
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class GenerateAIOverallRequest
    {
        [Required]
        public int SubmissionId { get; set; }
    }
}