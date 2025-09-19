using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string StudentCode { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<CourseInstructor> CourseInstructors { get; set; }
        public ICollection<CourseStudent> CourseStudents { get; set; }
        public ICollection<SystemConfig> SystemConfigs { get; set; }
        public ICollection<Submission> Submissions { get; set; }
        public ICollection<ReviewAssignment> ReviewAssignments { get; set; }
        public ICollection<RegradeRequest> RegradeRequestsReviewed { get; set; }
        public ICollection<RegradeRequest> ReviewedRegradeRequests { get; set; }
        public ICollection<Notification> ReceivedNotifications { get; set; }
        public ICollection<RubricTemplate> CreatedRubricTemplates { get; set; }
        public ICollection<Notification> SentNotifications { get; set; }
    }
}