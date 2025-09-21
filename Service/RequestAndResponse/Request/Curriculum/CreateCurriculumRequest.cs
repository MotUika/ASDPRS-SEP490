using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Curriculum
{
    public class CreateCurriculumRequest
    {
        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(20)]
        public string MajorCode { get; set; }

        [Required]
        [StringLength(100)]
        public string MajorName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}