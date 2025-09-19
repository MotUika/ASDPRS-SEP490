using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessObject.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}