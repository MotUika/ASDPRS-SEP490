using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentBasicResponse
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Guidelines { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime ReviewDeadline { get; set; }
        public DateTime FinalDeadline { get; set; }
        public string Status { get; set; }
        public int NumPeerReviewsRequired { get; set; }
        public int PendingReviewsCount { get; set; }
        public int CompletedReviewsCount { get; set; }

        public List<InstructorInfoBasic> Instructors { get; set; }

        public DateTime CourseInstanceStartDate { get; set; }
        public DateTime CourseInstanceEndDate { get; set; }
    }

    public class InstructorInfoBasic
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }
}