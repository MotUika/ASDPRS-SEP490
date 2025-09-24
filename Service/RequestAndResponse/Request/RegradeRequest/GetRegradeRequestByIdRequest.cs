using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class GetRegradeRequestByIdRequest
    {
        [Required(ErrorMessage = "RequestId is required")]
        public int RequestId { get; set; }
    }
}