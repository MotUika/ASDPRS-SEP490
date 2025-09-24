using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseInstructor
{
    public class CreateCourseInstructorRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public bool IsMainInstructor { get; set; } = false;
    }
}