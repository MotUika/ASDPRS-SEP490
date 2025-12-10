using System.ComponentModel.DataAnnotations;
namespace Service.RequestAndResponse.Request.Course
{
    public class UpdateCourseRequest
    {
        [Required]
        public int CourseId { get; set; }
        [StringLength(20)]
        public string CourseCode { get; set; }
        [StringLength(100)]
        public string CourseName { get; set; }
        public bool IsActive { get; set; }
    }
}