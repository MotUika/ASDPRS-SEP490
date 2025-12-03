using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.CourseInstance
{
    public class UpdateStatusRequest
    {
        public int CourseInstanceId { get; set; }
        public bool IsActive { get; set; }
    }
}
