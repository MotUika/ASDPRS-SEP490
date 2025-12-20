using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class Major
    {
        [Key]
        public int MajorId { get; set; }

        [Required]
        [StringLength(100)]
        public string MajorName { get; set; }

        [Required]
        [StringLength(10)]
        public string MajorCode { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();

    }
}