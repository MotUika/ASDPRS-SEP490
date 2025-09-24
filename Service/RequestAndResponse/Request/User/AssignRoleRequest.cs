namespace Service.RequestAndResponse.Request.User
{
    public class AssignRoleRequest
    {
        public int UserId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}