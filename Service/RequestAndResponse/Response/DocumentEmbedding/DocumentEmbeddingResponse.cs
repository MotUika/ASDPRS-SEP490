using System;

namespace Service.RequestAndResponse.Response.DocumentEmbedding
{
    public class DocumentEmbeddingResponse
    {
        public int EmbeddingId { get; set; }
        public string SourceType { get; set; }
        public int SourceId { get; set; }
        public string Content { get; set; }
        public byte[] ContentVector { get; set; }
        public DateTime CreatedAt { get; set; }

        public string SourceTitle { get; set; }
        public string SourceDescription { get; set; }
    }
}