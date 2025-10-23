using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.SystemConfig
{
    public class SystemConfigResponse
    {
        public int ConfigId { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UpdatedByUserId { get; set; }
    }
}
