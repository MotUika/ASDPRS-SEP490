using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Submission
{
    public class OverrideFinalScoreResponse
    {
        public int SubmissionId { get; set; }
        public decimal? OldFinalScore { get; set; }
        public decimal NewFinalScore { get; set; }
        public DateTime OverriddenAt { get; set; }
    }
}
