using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.User
{
    public class UpdateUserRequest
    {
        [Required]
        public int UserId { get; set; }

        public int CampusId { get; set; }

        public int? MajorId { get; set; }

        [StringLength(100)]
        public string Username { get; set; }

        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string StudentCode { get; set; }

        public string AvatarUrl { get; set; }

        public bool IsActive { get; set; }
    }
}