using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Curriculum
{
    public class UpdateCurriculumRequest
    {
        [Required]
        public int CurriculumId { get; set; }

        public int CampusId { get; set; }

        [StringLength(20)]
        public string MajorCode { get; set; }

        [StringLength(100)]
        public string MajorName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; }
    }
}