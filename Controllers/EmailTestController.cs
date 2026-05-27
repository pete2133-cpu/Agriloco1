using Agriloco.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Agriloco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;

        public EmailTestController(IEmailSender emailSender, IConfiguration config)
        {
            _emailSender = emailSender;
            _config = config;
        }

        // GET /api/EmailTest/config-check
        [HttpGet("config-check")]
        public IActionResult ConfigCheck()
        {
            // Don't return the password itself, just if it's present
            return Ok(new
            {
                env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                host = _config["Email:Host"],
                port = _config["Email:Port"],
                user = _config["Email:User"],
                from = _config["Email:From"],
                hasAppPassword = !string.IsNullOrWhiteSpace(_config["Email:AppPassword"])
            });
        }

        // GET /api/EmailTest/send?to=someone@gmail.com
        [HttpGet("send")]
        public async Task<IActionResult> Send([FromQuery] string to)
        {
            await _emailSender.SendAsync(to, "Agriloco test email", "If you got this, SMTP works.");
            return Ok(new { status = "sent" });
        }
    }
}