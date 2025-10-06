using System;
using System.Collections.Generic;

namespace Service.RequestAndResponse.Response.User
{
    public class UserResponse
    {
        public int Id { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StudentCode { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>(); // Thêm property Roles
    }
}