using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Submission
{
    public class OverrideFinalScoreRequest
    {
        public int SubmissionId { get; set; }
        public decimal NewFinalScore { get; set; }
        public int InstructorId { get; set; }
    }
}
