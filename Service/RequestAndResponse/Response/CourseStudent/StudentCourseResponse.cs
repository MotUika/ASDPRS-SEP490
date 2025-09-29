using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.CourseStudent
{
    public class StudentCourseResponse
    {
        public int CourseInstanceId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }
        public string CampusName { get; set; }
        public string SemesterName { get; set; }
        public string Status { get; set; }
    }
}
