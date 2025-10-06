using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Curriculum
{
    public class UpdateCurriculumRequest
    {
        [Required]
        public int CurriculumId { get; set; }

        public int CampusId { get; set; }

        public int MajorId { get; set; }

        [StringLength(100)]
        public string CurriculumName { get; set; }

        [StringLength(20)]
        public string CurriculumCode { get; set; }

        public int TotalCredits { get; set; }

        public bool IsActive { get; set; }
    }
}