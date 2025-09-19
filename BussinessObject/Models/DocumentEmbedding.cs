using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class DocumentEmbedding
    {
        [Key]
        public int EmbeddingId { get; set; }

        [Required]
        [StringLength(50)]
        public string SourceType { get; set; }

        [Required]
        public int SourceId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        public byte[] ContentVector { get; set; } // Assuming varbinary in SQL, use byte[] in C#

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Note: Relationships to Submission, Review, AISummary are one-to-many, but since SourceType/SourceId are generic, no explicit FK here.
        // You may need to configure this in DbContext if needed.
    }
}
