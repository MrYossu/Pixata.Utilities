using LanguageExt;
using MailKit.Net.Smtp;
using MimeKit;
using static LanguageExt.Prelude;

namespace Pixata.Email {
  public class PixataEmailService {
    private readonly SmtpSettings _smtpSettings;

    public PixataEmailService(SmtpSettings smtpSettings) =>
      _smtpSettings = smtpSettings;

    public TryAsync<Unit> SendEmailAsync(string email, string subject, string htmlMessage) =>
      SendEmailAsync(new(subject, htmlMessage, email));

    public TryAsync<Unit> SendEmailAsync(EmailParameters emailParameters) =>
      TryAsync(async () => {
        MimeMessage msg = CreateMailMessage(emailParameters);
        using SmtpClient client = new();
        await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, false);
        await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
        return unit;
      });

    /*
    public Task<string> SendEmailAsync(string email, string subject, string htmlMessage) {
      EmailParameters parameters = new(subject, htmlMessage, email);
      return SendEmailAsync(parameters);
    }

    public async Task<string> SendEmailAsync(EmailParameters emailParameters) {
      try {
        MimeMessage msg = CreateMailMessage(emailParameters);
        using SmtpClient client = new();
        await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, false);
        await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
        return "OK";
      }
      catch (Exception ex) {
        return ex.Message;
      }
    }
    */

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