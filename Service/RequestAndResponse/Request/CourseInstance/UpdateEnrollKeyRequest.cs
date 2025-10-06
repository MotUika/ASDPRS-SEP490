using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.CourseInstance
{
    public class UpdateEnrollKeyRequest
    {
        public string NewKey { get; set; }
        public int UserId { get; set; }
    }
}
