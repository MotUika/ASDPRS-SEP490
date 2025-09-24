using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseStudent
{
    public class CreateCourseStudentRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Enrolled";

        public decimal? FinalGrade { get; set; }

        public bool IsPassed { get; set; } = false;

        public int? ChangedByUserId { get; set; }
    }
}