using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.CourseStudent
{
    public class ImportStudentsMultipleRequest
    {
        public IFormFile File { get; set; }
        public int CampusId { get; set; }
        public int? ChangedByUserId { get; set; }
    }
}
