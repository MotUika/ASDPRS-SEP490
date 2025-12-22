using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseStudent
{
    public class CreateCourseStudentRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

       
        public int UserId { get; set; }

        public string StudentCode { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        public int? ChangedByUserId { get; set; }
    }
}