using MailKit.Security;
using MimeKit;
using Service.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly string _smtpServer;
        private readonly int _smtpPort;

        public EmailService(string fromEmail, string password, string smtpServer = "smtp.gmail.com", int smtpPort = 587)
        {
            _fromEmail = fromEmail;
            _password = password;
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
        }

        public async Task<bool> SendEmail(string email, string subject, string htmlContent)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_fromEmail));
                message.Subject = subject;
                message.To.Add(MailboxAddress.Parse(email));
                message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = htmlContent
                };

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_fromEmail, _password);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
