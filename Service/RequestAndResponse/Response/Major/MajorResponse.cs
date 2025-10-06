using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.Major
{
    public class MajorResponse
    {
        public int MajorId { get; set; }
        public string MajorName { get; set; }
        public string MajorCode { get; set; }
        public bool IsActive { get; set; }
        public int CurriculumCount { get; set; }
    }
}
