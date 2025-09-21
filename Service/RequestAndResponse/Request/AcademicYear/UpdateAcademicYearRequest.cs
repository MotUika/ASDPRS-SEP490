using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.AcademicYear
{
    public class UpdateAcademicYearRequest
    {
        [Required]
        public int AcademicYearId { get; set; }

        public int CampusId { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}