using System.Threading.Tasks;

namespace Agriloco.Api.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string bodyText);
    }
}