using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Rubric
{
    public class UpdateRubricRequest
    {
        [Required]
        public int RubricId { get; set; }

        public int? TemplateId { get; set; }

        public int? AssignmentId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsModified { get; set; }
    }
}