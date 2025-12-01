using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Notification
{
    public class NotificationResponse
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int? SenderUserId { get; set; }
        public int? AssignmentId { get; set; }
        public int? SubmissionId { get; set; }
        public int? ReviewAssignmentId { get; set; }
        public int? CourseId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        // Optional: User names, etc., for FE
    }
}
