using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Review
{
    public class UpdateStudentReviewRequest
    {
        public int ReviewId { get; set; }
        public int ReviewerUserId { get; set; }
        public string GeneralFeedback { get; set; }
        public List<CriteriaFeedbackRequest> CriteriaFeedbacks { get; set; }
    }
}
