using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? SenderUserId { get; set; }

        public int? AssignmentId { get; set; }

        public int? SubmissionId { get; set; }

        public int? ReviewAssignmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [Required]
        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public virtual User SenderUser { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public virtual Assignment Assignment { get; set; }

        [ForeignKey(nameof(SubmissionId))]
        public virtual Submission Submission { get; set; }

        [ForeignKey(nameof(ReviewAssignmentId))]
        public virtual ReviewAssignment ReviewAssignment { get; set; }
    }
}
