using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Agriloco.Api.Services
{
    public class DebugEmailSender : IEmailSender
    {
        private readonly ILogger<DebugEmailSender> _logger;

        public DebugEmailSender(ILogger<DebugEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string bodyText)
        {
            _logger.LogInformation("DEBUG EMAIL SENDER: To={To} Subject={Subject}\n{Body}",
                toEmail, subject, bodyText);

            // Always "succeeds"
            return Task.CompletedTask;
        }
    }
}