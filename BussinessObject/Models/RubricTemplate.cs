using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessObject.Models
{
    public class RubricTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; }

        public ICollection<Rubric> Rubrics { get; set; }
        public ICollection<CriteriaTemplate> CriteriaTemplates { get; set; }
    }
}