using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Notification
{
    public class SendAnnouncementToUsersRequest
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public List<int> UserIds { get; set; }
    }
}
