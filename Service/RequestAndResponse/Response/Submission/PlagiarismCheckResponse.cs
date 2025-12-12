using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Submission
{
    public class PlagiarismCheckResponse
    {
        public bool RelevantContent { get; set; }

        public string ContentChecking { get; set; }

        public double PlagiarismContent { get; set; }

        public double Threshold { get; set; }
    }
}
