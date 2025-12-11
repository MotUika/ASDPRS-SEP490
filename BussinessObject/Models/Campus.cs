using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class Campus
    {
        [Key]
        public int CampusId { get; set; }

        [Required]
        [StringLength(100)]
        public string CampusName { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        public ICollection<User> Users { get; set; }
        public ICollection<AcademicYear> AcademicYears { get; set; }
        public ICollection<CourseInstance> CourseInstances { get; set; }
    }
}