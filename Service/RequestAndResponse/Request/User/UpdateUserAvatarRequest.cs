using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.User
{
    public class UpdateUserAvatarRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string AvatarUrl { get; set; }
    }
}
