using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.RegradeRequest
{
    public class RegradeRequestListResponse
    {
        public List<RegradeRequestResponse> Requests { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}