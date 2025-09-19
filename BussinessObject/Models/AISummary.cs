using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class AISummary
    {
        [Key]
        public int SummaryId { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        [Required]
        [StringLength(50)]
        public string SummaryType { get; set; }

        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SubmissionId))]
        public virtual Submission Submission { get; set; }

        // Navigation properties
        public virtual ICollection<DocumentEmbedding> DocumentEmbeddings { get; set; } = new List<DocumentEmbedding>();
    }
}
