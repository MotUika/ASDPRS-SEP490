using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class CriteriaFeedback
    {
        [Key]
        public int CriteriaFeedbackId { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [Required]
        public int CriteriaId { get; set; }

        public decimal? ScoreAwarded { get; set; }

        [StringLength(500)]
        public string Feedback { get; set; }

        [StringLength(50)]
        public string FeedbackSource { get; set; }

        [ForeignKey(nameof(ReviewId))]
        public virtual Review Review { get; set; }

        [ForeignKey(nameof(CriteriaId))]
        public virtual Criteria Criteria { get; set; }
    }
}
