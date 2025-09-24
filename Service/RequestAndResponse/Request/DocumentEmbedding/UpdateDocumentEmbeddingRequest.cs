using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.DocumentEmbedding
{
    public class UpdateDocumentEmbeddingRequest
    {
        [Required]
        public int EmbeddingId { get; set; }

        [StringLength(2000)]
        public string Content { get; set; }

        public byte[] ContentVector { get; set; }
    }
}
