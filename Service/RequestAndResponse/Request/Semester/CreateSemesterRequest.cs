using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Semester
{
    public class CreateSemesterRequest
    {
        [Required]
        public int AcademicYearId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}