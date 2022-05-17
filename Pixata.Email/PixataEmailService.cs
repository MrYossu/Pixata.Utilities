using LanguageExt;
using MailKit.Net.Smtp;
using MimeKit;
using static LanguageExt.Prelude;

namespace Pixata.Email {
  public class PixataEmailService : PixataEmailServiceInterface {
    private readonly SmtpSettings _smtpSettings;

    public PixataEmailService(SmtpSettings smtpSettings) =>
      _smtpSettings = smtpSettings;

    /// <summary>
    /// Send an email from the user specified in the settings to the email passed in
    /// </summary>
    /// <param name="email">The recipient email address</param>
    /// <param name="subject">The email subject line</param>
    /// <param name="htmlMessage">An HTML string for the body of the email</param>
    /// <param name="useSsl">An optiona bool that specifies whether to use SSL or not. Default true</param>
    /// <returns>Unit</returns>
    public TryAsync<Unit> SendEmailAsync(string email, string subject, string htmlMessage) =>
      SendEmailAsync(new(subject, htmlMessage, email));

    /// <summary>
    /// Send an email from the user specified in the settings to the email passed in. Allows multiple recipients and attachments
    /// </summary>
    /// <param name="emailParameters">An EmailParameters object, which contains all the data for the email</param>
    /// <returns>Unit</returns>
    public TryAsync<Unit> SendEmailAsync(EmailParameters emailParameters) =>
      TryAsync(async () => {
        MimeMessage msg = CreateMailMessage(emailParameters);
        using SmtpClient client = new();
        await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, _smtpSettings.UseSsl);
        await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
        return unit;
      });

    private MimeMessage CreateMailMessage(EmailParameters parameters) {
      MimeMessage mm = new() {
        Subject = parameters.Subject,
      };
      mm.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
      parameters.Recipients.ForEach(r => mm.To.Add(r));
      if (parameters.IsReplyToSet) {
        mm.ReplyTo.Add(parameters.ReplyTo);
      }
      BodyBuilder builder = new() {
        HtmlBody = parameters.Body
      };
      foreach ((string fileName, string mimeType, byte[] data) in parameters.Attachments) {
        builder.Attachments.Add(fileName, data, ContentType.Parse(mimeType));
      }
      mm.Body = builder.ToMessageBody();
      return mm;
    }
  }
}