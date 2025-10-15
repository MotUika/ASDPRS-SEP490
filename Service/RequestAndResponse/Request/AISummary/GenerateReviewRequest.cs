using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.AISummary
{
    public class GenerateReviewRequest
    {
        public int SubmissionId { get; set; }
        public bool ReplaceExisting { get; set; } = true;
    }
}
