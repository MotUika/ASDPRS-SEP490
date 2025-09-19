using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BussinessObject.Models;

namespace BussinessObject.Models
{
    public class CourseInstructor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(CourseInstanceId))]
        public virtual CourseInstance CourseInstance { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        // Navigation properties
        public virtual ICollection<RegradeRequest> ReviewedRegradeRequests { get; set; } = new List<RegradeRequest>();
    }
}