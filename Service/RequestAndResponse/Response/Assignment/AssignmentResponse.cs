using System;
using System.Collections.Generic;
using Service.RequestAndResponse.Response.Rubric;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class AssignmentResponse
    {
        public int AssignmentId { get; set; }
        public int CourseInstanceId { get; set; }
        public int? RubricId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Guidelines { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public DateTime? FinalDeadline { get; set; }
        public int NumPeerReviewsRequired { get; set; }
        public bool AllowCrossClass { get; set; }
        public bool IsBlindReview { get; set; }
        public decimal InstructorWeight { get; set; }
        public decimal PeerWeight { get; set; }
        public bool IncludeAIScore { get; set; }
        public decimal Weight { get; set; }
        public string GradingScale { get; set; }


        // Additional info
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public string SectionCode { get; set; }
        public string CampusName { get; set; }
        public RubricResponse Rubric { get; set; }
        public int SubmissionCount { get; set; }
        public int ReviewCount { get; set; }
        public string Status { get; set; }       // trạng thái thực (Active, Closed, InReview,...)
        public string UiStatus { get; set; }     // hiển thị cho UI (Due Soon, Overdue,...)
        public bool IsActive => StartDate == null || DateTime.UtcNow >= StartDate;
        public bool IsOverdue => DateTime.UtcNow > Deadline;
        public int DaysUntilDeadline => (int)(Deadline - DateTime.UtcNow).TotalDays;
    }
}