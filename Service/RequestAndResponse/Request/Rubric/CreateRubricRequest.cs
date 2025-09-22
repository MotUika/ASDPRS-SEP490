using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Rubric
{
    public class CreateRubricRequest
    {
        public int? TemplateId { get; set; }

        public int? AssignmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public bool IsModified { get; set; } = false;
    }
}