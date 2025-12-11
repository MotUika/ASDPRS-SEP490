using BussinessObject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    [StringLength(20)]
    public string CourseCode { get; set; }

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<CourseInstance> CourseInstances { get; set; } = new List<CourseInstance>();
    public virtual ICollection<RubricTemplate> RubricTemplates { get; set; } = new List<RubricTemplate>();
}