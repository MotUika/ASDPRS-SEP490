using BussinessObject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    public int CurriculumId { get; set; }

    [Required]
    [StringLength(20)]
    public string CourseCode { get; set; }

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; }

    // REMOVED: Description field
    [Required]
    public int Credits { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(CurriculumId))]
    public virtual Curriculum Curriculum { get; set; }

    public virtual ICollection<CourseInstance> CourseInstances { get; set; } = new List<CourseInstance>();
}