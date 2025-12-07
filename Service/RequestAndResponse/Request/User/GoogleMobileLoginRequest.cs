using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.User
{
    public class GoogleMobileLoginRequest
    {
        [Required]
        public string IdToken { get; set; }
    }
}
