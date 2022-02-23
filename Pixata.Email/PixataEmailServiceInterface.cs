using System.Threading.Tasks;

namespace Pixata.Email {
  public interface PixataEmailServiceInterface {
    Task<string> SendEmailAsync(string email, string subject, string htmlMessage);
    Task<string> SendEmailAsync(EmailParameters emailParameters);
  }
}