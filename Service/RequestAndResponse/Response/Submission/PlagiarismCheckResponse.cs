using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Submission
{
    public class PlagiarismCheckResponse
    {
        public double MaxSimilarity { get; set; }
        public bool IsAboveThreshold { get; set; }
        public double Threshold { get; set; }
    }
}
