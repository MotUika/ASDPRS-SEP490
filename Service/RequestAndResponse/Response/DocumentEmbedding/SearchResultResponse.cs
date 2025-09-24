using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.DocumentEmbedding
{
    public class SearchResultResponse
    {
        public List<DocumentEmbeddingResponse> Results { get; set; } = new List<DocumentEmbeddingResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}