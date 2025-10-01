using BussinessObject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Rubric
{
    [Key]
    public int RubricId { get; set; }

    public int? TemplateId { get; set; }

    public int? AssignmentId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    // REMOVED: Description field
    [Required]
    public bool IsModified { get; set; } = false;

    [ForeignKey(nameof(TemplateId))]
    public virtual RubricTemplate Template { get; set; }

    [ForeignKey(nameof(AssignmentId))]
    public virtual Assignment Assignment { get; set; }

    public virtual ICollection<Criteria> Criteria { get; set; } = new List<Criteria>();
}