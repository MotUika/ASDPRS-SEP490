using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseInstance
{
    public class UpdateCourseInstanceRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        public int CourseId { get; set; }

        public int SemesterId { get; set; }

        public int CampusId { get; set; }

        [StringLength(20)]
        public string SectionCode { get; set; }

        [StringLength(50)]
        public string EnrollmentPassword { get; set; }

        public bool RequiresApproval { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}