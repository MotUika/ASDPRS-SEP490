using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Response.User
{
    // Service/RequestAndResponse/Response/User/AccountStatisticsResponse.cs
    namespace Service.RequestAndResponse.Response.User
    {
        public class AccountStatisticsResponse
        {
            public int TotalAccounts { get; set; }
            public int AdminAccounts { get; set; }
            public int StudentAccounts { get; set; }
            public int InstructorAccounts { get; set; }
        }
    }
}
