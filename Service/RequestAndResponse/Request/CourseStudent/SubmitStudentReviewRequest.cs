using Service.RequestAndResponse.Request.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.CourseStudent
{
    public class SubmitStudentReviewRequest
    {
        public int ReviewAssignmentId { get; set; }
        public int ReviewerUserId { get; set; }
        public string GeneralFeedback { get; set; }
        public List<CriteriaFeedbackRequest> CriteriaFeedbacks { get; set; }
    }

    public class ReviewCriteriaFeedbackRequest
    {
        public int CriteriaId { get; set; }
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
    }
}
