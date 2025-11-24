using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.Statistic
{
    public class AssignmentDistributionResponse
    {
        public int AssignmentId { get; set; }            // Id của assignment, =0 nếu là tổng hợp
        public string AssignmentTitle { get; set; }      // Tiêu đề assignment, "Total Assignment" nếu là tổng hợp

        public List<DistributionItem> Distribution { get; set; } = new List<DistributionItem>();

        // --- Trường mới để phân biệt tổng hợp ---
        public bool IsTotal { get; set; }               // true nếu là tổng hợp
        public int TotalAssignmentCount { get; set; }   // tổng số assignment, chỉ dùng khi IsTotal = true
    }


}