using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.RubricTemplate
{
    public class CreateRubricTemplateRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public int? MajorId { get; set; }

        //public bool IsPublic { get; set; } = false;
        public int? CourseId { get; set; }
        [Required]
        public int CreatedByUserId { get; set; }
    }
}