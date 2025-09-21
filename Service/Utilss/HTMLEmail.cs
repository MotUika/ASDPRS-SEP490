using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Utilss
{
    public class HTMLEmail
    {
        public static string CreateAccountEmail(string fullname, string email, string password)
        {
            var html = $@"<div style='font-family: Arial, sans-serif; color: #333;'>
                         <p>Dear {fullname},</p>
                         <hr>
                         <p>
                            You have requested to create account on {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} +07<br/>
                            This is your login information:<br/>
                                Email:    <strong>{email}</strong><br/>
                                Password: <strong>{password}</strong><br/>
                            If this is not requested by you, please contact immediately.
                        </p>
                        <p>This is a computer-generated email. Please do not reply this email.</p>
                        <p>Best Regards<br/>
                    </div>";
            return html;
        }
    }
}
