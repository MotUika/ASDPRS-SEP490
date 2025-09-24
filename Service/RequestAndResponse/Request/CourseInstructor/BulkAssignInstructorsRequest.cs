using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseInstructor
{
    public class BulkAssignInstructorsRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public List<int> InstructorIds { get; set; } = new List<int>();

        public bool IsMainInstructor { get; set; } = false;
    }
}