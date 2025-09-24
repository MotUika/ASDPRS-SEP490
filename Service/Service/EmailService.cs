using MailKit.Security;
using MimeKit;
using Service.IService;
using Microsoft.Extensions.Configuration; // Add this using
using System;
using System.Threading.Tasks;

namespace Service.Service
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly string _smtpServer;
        private readonly int _smtpPort;

        public EmailService(IConfiguration configuration)
        {
            _fromEmail = configuration["EmailSettings:SenderEmail"];
            _password = configuration["EmailSettings:SenderPassword"];
            _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
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