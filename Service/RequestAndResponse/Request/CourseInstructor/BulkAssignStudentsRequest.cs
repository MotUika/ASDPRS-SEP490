using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.CourseStudent
{
    public class BulkAssignStudentsRequest
    {
        [Required]
        public int CourseInstanceId { get; set; }

        [Required]
        public List<int> StudentIds { get; set; } = new List<int>();

        [StringLength(50)]
        public string Status { get; set; } = "Enrolled";

        public int? ChangedByUserId { get; set; }
    }
}