using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Agriloco.Api.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string bodyText)
        {
            var host = _config["Email:Host"] ?? "smtp.gmail.com";
            var portStr = _config["Email:Port"] ?? "587";
            var user = _config["Email:User"];
            var pass = _config["Email:AppPassword"];
            var from = _config["Email:From"] ?? user;

            if (string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(from))
            {
                throw new System.Exception("Missing Email config. Need Email:User, Email:AppPassword, Email:From.");
            }

            if (!int.TryParse(portStr, out var port))
                port = 587;

            using var msg = new MailMessage();
            msg.From = new MailAddress(from);
            msg.To.Add(toEmail);
            msg.Subject = subject;
            msg.Body = bodyText;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };

            await client.SendMailAsync(msg);
        }
    }
}