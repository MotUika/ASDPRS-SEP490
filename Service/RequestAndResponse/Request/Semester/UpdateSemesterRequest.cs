using System;
using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Semester
{
    public class UpdateSemesterRequest
    {
        [Required]
        public int SemesterId { get; set; }

        public int AcademicYearId { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}