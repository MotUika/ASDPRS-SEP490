using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class SystemConfig
    {
        [Key]
        public int ConfigId { get; set; }

        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; }

        [StringLength(500)]
        public string ConfigValue { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UpdatedByUserId { get; set; }

        [ForeignKey("UpdatedByUserId")]
        public User UpdatedByUser { get; set; }
    }
}