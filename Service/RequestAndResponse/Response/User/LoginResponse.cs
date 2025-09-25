namespace Service.RequestAndResponse.Response.User
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StudentCode { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int? CampusId { get; set; }
    }
}