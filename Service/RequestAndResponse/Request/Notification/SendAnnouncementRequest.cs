using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Request.Notification
{
    public class SendAnnouncementRequest
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public int? SenderUserId { get; set; }
    }

}
