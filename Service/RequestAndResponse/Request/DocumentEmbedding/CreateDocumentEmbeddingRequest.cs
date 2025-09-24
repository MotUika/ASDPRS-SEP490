using BussinessObject.Models;
using Service.RequestAndResponse.Request.DocumentEmbedding;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.DocumentEmbedding
{
    public class CreateDocumentEmbeddingRequest
    {
        [Required]
        [StringLength(50)]
        public string SourceType { get; set; } // "Submission", "Review", "AISummary", etc.

        [Required]
        public int SourceId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        public byte[] ContentVector { get; set; }
    }
}