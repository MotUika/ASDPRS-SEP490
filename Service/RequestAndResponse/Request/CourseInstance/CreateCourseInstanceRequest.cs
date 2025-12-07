using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseInstance
{
    public class CreateCourseInstanceRequest
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(20)]
        public string SectionCode { get; set; }

        [StringLength(50)]
        public string EnrollmentPassword { get; set; }

        [Required]
        public bool RequiresApproval { get; set; } = false;
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }
}