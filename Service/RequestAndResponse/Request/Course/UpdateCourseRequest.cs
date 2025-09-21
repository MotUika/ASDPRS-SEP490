using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Course
{
    public class UpdateCourseRequest
    {
        [Required]
        public int CourseId { get; set; }

        public int CurriculumId { get; set; }

        [StringLength(20)]
        public string CourseCode { get; set; }

        [StringLength(100)]
        public string CourseName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int Credits { get; set; }

        public bool IsActive { get; set; }
    }
}