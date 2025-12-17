using BussinessObject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RubricTemplate
{
    [Key]
    public int TemplateId { get; set; }

    [Required]
    public string Title { get; set; }

    public bool IsPublic { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    public User CreatedByUser { get; set; }

    public int? MajorId { get; set; }

    [ForeignKey(nameof(MajorId))]
    public Major? Major { get; set; }

    public int? CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    public ICollection<Rubric> Rubrics { get; set; }
    public ICollection<CriteriaTemplate> CriteriaTemplates { get; set; }
}