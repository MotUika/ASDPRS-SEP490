using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Course
{
    public class CreateCourseRequest
    {
        [Required]
        public int CurriculumId { get; set; }

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }

        [Required]
        public int Credits { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}