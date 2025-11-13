using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.AISummary
{
    public class AIOverallResponse
    {
        public string Summary { get; set; }
        public bool IsRelevant { get; set; } = true;
        public string ErrorMessage { get; set; }
    }
}
