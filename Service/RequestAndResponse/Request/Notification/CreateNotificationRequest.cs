using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Notification
{
    public class CreateNotificationRequest
    {
        public int UserId { get; set; }
        public int? SenderUserId { get; set; }
        public int? AssignmentId { get; set; }
        public int? SubmissionId { get; set; }
        public int? ReviewAssignmentId { get; set; }
        public int? CourseInstanceId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }  // e.g., "AssignmentNew", "DeadlineReminder", "MissingSubmission"
    }
}
