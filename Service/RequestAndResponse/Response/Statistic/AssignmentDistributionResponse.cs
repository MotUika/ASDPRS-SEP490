using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentDistributionResponse
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }

        public List<DistributionItem> Distribution { get; set; }
            = new List<DistributionItem>();
    }

}
