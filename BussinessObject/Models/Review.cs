using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int ReviewAssignmentId { get; set; }

        public decimal? OverallScore { get; set; }

        [StringLength(1000)]
        public string GeneralFeedback { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [Required]
        [StringLength(50)]
        public string ReviewType { get; set; }

        [StringLength(50)]
        public string FeedbackSource { get; set; }

        [ForeignKey(nameof(ReviewAssignmentId))]
        public virtual ReviewAssignment ReviewAssignment { get; set; }

        // Navigation properties
        public virtual ICollection<CriteriaFeedback> CriteriaFeedbacks { get; set; } = new List<CriteriaFeedback>();
        public virtual ICollection<DocumentEmbedding> DocumentEmbeddings { get; set; } = new List<DocumentEmbedding>();
    }
}
