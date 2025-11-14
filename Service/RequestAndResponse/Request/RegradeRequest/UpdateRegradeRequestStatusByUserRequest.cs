namespace Service.RequestAndResponse.Request.RegradeRequest
{
    public class UpdateRegradeRequestStatusByUserRequest
    {
        
        public int RequestId { get; set; }

        
        public string Status { get; set; }

        
        public string ResolutionNotes { get; set; }

        
        public int? ReviewedByUserId { get; set; }
    }
}
