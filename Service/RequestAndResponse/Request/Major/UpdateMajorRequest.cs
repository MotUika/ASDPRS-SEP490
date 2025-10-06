using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Major
{
    public class UpdateMajorRequest
    {
        [Required]
        public int MajorId { get; set; }

        [StringLength(100)]
        public string MajorName { get; set; }

        [StringLength(10)]
        public string MajorCode { get; set; }

        public bool IsActive { get; set; }
    }
}
