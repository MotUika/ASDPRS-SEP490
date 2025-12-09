using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Assignment
{
    public class CourseAssignmentsWrapperResponse
    {
        public int CourseInstanceId { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public string SectionCode { get; set; }
        public string InstructorName { get; set; }
        public string InstructorEmail { get; set; }
        public string InstructorAvatar { get; set; }

        public List<AssignmentBasicResponse> Assignments { get; set; }
    }
}
