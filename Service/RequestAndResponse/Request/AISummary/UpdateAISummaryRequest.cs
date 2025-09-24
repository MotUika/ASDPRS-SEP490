using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class UpdateAISummaryRequest
    {
        [Required]
        public int SummaryId { get; set; }

        [StringLength(2000)]
        public string Content { get; set; }

        [StringLength(50)]
        public string SummaryType { get; set; }
    }
}