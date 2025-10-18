using Service.RequestAndResponse.Response.Criteria;
using Service.RequestAndResponse.Response.Submission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.ReviewAssignment
{
    public class ReviewAssignmentDetailsResponse
    {
        public ReviewAssignmentResponse ReviewAssignment { get; set; }
        public SubmissionResponse Submission { get; set; }
        public string AssignmentTitle { get; set; }
        public string AssignmentDescription { get; set; }
        public List<CriteriaResponse> RubricCriteria { get; set; }
        public bool IsBlindReview { get; set; }
    }
}
