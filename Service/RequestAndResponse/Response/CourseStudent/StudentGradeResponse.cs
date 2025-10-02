using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.CourseStudent
{
    public class StudentGradeResponse
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public decimal TotalGrade { get; set; }
    }
}
