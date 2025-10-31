
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class GenerateAICriteriaRequest
    {
        [Required]
        public int SubmissionId { get; set; }
    }
}
