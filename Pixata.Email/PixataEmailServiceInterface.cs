using LanguageExt;

namespace Pixata.Email {
  public interface PixataEmailServiceInterface {
    TryAsync<Unit> SendEmailAsync(string email, string subject, string htmlMessage);
    TryAsync<Unit> SendEmailAsync(EmailParameters emailParameters);
  }
}