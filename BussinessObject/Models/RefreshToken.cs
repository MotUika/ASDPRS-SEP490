using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessObject.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string JwtId { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime ExpiredAt { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}
