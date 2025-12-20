using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.CourseInstance
{
    public class CourseInstanceImportResponse
    {
        // ===== Identity =====
        public int CourseInstanceId { get; set; }

        // ===== Course =====
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }

        // ===== Semester =====
        public int SemesterId { get; set; }
        public string SemesterName { get; set; }

        // ===== Academic Year =====
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; }

        // ===== Campus =====
        public int CampusId { get; set; }
        public string CampusName { get; set; }

        // ===== Class Info =====
        public string SectionCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // ===== Instructor =====
        public int InstructorId { get; set; }
        public string InstructorEmail { get; set; }

        // ===== Meta =====
        public DateTime CreatedAt { get; set; }
    }

}
