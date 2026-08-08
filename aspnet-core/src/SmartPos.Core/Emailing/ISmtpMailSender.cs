using System.Threading.Tasks;

namespace SmartPos.Emailing
{
    public interface ISmtpMailSender
    {
        Task SendAsync(string toAddress, string subject, string body, bool isBodyHtml = false);
    }
}
