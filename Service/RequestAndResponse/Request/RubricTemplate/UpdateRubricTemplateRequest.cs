using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RubricTemplate
{
    public class UpdateRubricTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [StringLength(200)]
        public string Title { get; set; }
        public int? MajorId { get; set; }

        public bool IsPublic { get; set; }
    }
}