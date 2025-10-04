using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Curriculum
{
    public class CreateCurriculumRequest
    {
        [Required]
        public int CampusId { get; set; }

        [Required]
        public int MajorId { get; set; }

        [Required]
        [StringLength(100)]
        public string CurriculumName { get; set; }

        [Required]
        [StringLength(20)]
        public string CurriculumCode { get; set; }

        public int TotalCredits { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}