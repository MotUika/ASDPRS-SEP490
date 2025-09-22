using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RubricTemplate
{
    public class UpdateRubricTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; }
    }
}