using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AcademicYear
{
    public class CreateAcademicYearRequest
    {
        [Required]
        public int CampusId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}