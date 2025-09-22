using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RubricTemplate
{
    public class CreateRubricTemplateRequest
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }
    }
}