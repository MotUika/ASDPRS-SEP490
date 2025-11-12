using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.CourseStudent
{

        public class MyCourseResponse
    {
            public int CourseInstanceId { get; set; }
            public int CourseId { get; set; }
            public string CourseCode { get; set; }
            public string CourseName { get; set; }
            public int SemesterId { get; set; }
            public string SemesterName { get; set; }
            public int CampusId { get; set; }
            public string CampusName { get; set; }
            public string SectionCode { get; set; }
            public string EnrollmentPassword { get; set; }
            public bool RequiresApproval { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<string> InstructorNames { get; set; } = new List<string>();
            public int InstructorCount { get; set; }
            public int StudentCount { get; set; }
            public int AssignmentCount { get; set; }

        }
    }
