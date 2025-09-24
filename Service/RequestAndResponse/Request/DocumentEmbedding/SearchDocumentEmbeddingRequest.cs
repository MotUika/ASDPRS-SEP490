using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.DocumentEmbedding
{
    public class SearchDocumentEmbeddingRequest
    {
        [StringLength(100)]
        public string SearchTerm { get; set; }

        [StringLength(50)]
        public string SourceType { get; set; }

        public int? SourceId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}