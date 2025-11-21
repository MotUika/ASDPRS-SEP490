using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.SystemConfig
{
    public class ImportantConfigsResponse
    {
        public decimal ScorePrecision { get; set; }
        public int AISummaryMaxTokens { get; set; }
        public int AISummaryMaxWords { get; set; }
        public decimal DefaultPassThreshold { get; set; }
        public int RegradeProcessingDeadlineDays { get; set; }
    }
}
