using BussinessObject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Assignment
{
    [Key]
    public int AssignmentId { get; set; }

    [Required]
    public int CourseInstanceId { get; set; }

    public int? RubricId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(1000)]
    public string Guidelines { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 3 mốc thời gian quan trọng
    public DateTime? StartDate { get; set; }  // Thời điểm bắt đầu được nộp
    [Required]
    public DateTime Deadline { get; set; }    // Hạn cuối nộp
    public DateTime? FinalDeadline { get; set; } // Hết được nộp (nếu có)

    public DateTime? ReviewDeadline { get; set; }

    [Required]
    public int NumPeerReviewsRequired { get; set; } = 0;

    [Required]
    public bool AllowCrossClass { get; set; } = false;

    [Required]
    public bool IsBlindReview { get; set; } = false;

    [Required]
    public decimal InstructorWeight { get; set; } = 0;

    [Required]
    public string GradingScale { get; set; } = "Scale10";  // "PassFail" or "Scale10"

    [Required]
    public decimal Weight { get; set; } = 0;  // Percentage weight in course (0-100)

    [Required]
    public decimal PeerWeight { get; set; } = 0;

    [Required]
    public bool IncludeAIScore { get; set; } = false;

    // Trạng thái assignment
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft";

    // Clone tracking
    public int? ClonedFromAssignmentId { get; set; }

    [ForeignKey(nameof(CourseInstanceId))]
    public virtual CourseInstance CourseInstance { get; set; }

    [ForeignKey(nameof(RubricId))]
    public virtual Rubric Rubric { get; set; }

    [ForeignKey(nameof(ClonedFromAssignmentId))]
    public virtual Assignment ClonedFromAssignment { get; set; }

    // Navigation properties
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Assignment> ClonedAssignments { get; set; } = new List<Assignment>();
}