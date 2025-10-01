using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.User
{
    public class CreateUserRequest
    {
        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        public string Password { get; set; }

        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string? StudentCode { get; set; }

        public string AvatarUrl { get; set; }

        public string Role { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}