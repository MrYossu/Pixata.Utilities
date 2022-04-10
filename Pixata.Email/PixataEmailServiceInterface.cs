using LanguageExt;

namespace Pixata.Email {
  public interface PixataEmailServiceInterface {
    TryAsync<Unit> SendEmailAsync(string email, string subject, string htmlMessage, bool useSsl = true);
    TryAsync<Unit> SendEmailAsync(EmailParameters emailParameters, bool useSsl = true);
  }
}