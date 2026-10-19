using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Pixata.Extensions;

namespace Pixata.Email {
  public class PixataEmailService(SmtpSettings smtpSettings) : PixataEmailServiceInterface {
    /// <summary>
    /// The settings for the SMTP server. You would normally pass these in to the constructor, but they are exposed as a property to allow you to override them in case you want to send emails from a different account
    /// </summary>
    public SmtpSettings SmtpSettings { get; set; } = smtpSettings;

    /// <summary>
    /// Send an email from the user specified in the settings to the email passed in
    /// </summary>
    /// <param name="email">The recipient email address</param>
    /// <param name="subject">The email subject line</param>
    /// <param name="htmlMessage">An HTML string for the body of the email</param>
    /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;</returns>
    public Task<ApiResponse<Yunit>> SendEmailAsync(string email, string subject, string htmlMessage) =>
      SendEmailAsync(new(subject, htmlMessage, email));

    /// <summary>
    /// Send an email from the user specified in the settings to the email passed in. Allows multiple recipients and attachments
    /// </summary>
    /// <param name="emailParameters">An EmailParameters object, which contains all the data for the email</param>
    /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;</returns>
    public Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters) =>
      SendMessageAsync(CreateMailMessage(emailParameters), client => client.ConnectAsync(SmtpSettings.Server, SmtpSettings.Port, SmtpSettings.EffectiveSocketOptions()), includeExceptionType: true);

    /// <summary>
    /// Send an email from the user specified in the settings to the email passed in. Allows multiple recipients and attachments
    /// </summary>
    /// <param name="emailParameters">An EmailParameters object, which contains all the data for the email</param>
    /// <param name="secureSocketOptions">A member of the MailKit.Security.SecureSocketOptions</param>
    /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;</returns>
    public Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters, SecureSocketOptions secureSocketOptions) =>
      SendMessageAsync(CreateMailMessage(emailParameters), client => client.ConnectAsync(SmtpSettings.Server, SmtpSettings.Port, secureSocketOptions), includeExceptionType: false);

    /// <summary>
    /// Send several emails over a single connection to the SMTP server, which is much quicker than calling SendEmailAsync once for each, as
    /// the connection, TLS handshake and authentication are only done once. The emails are sent one after the other, so this doesn't send
    /// any faster than one at a time would, which keeps within the sending rate of providers such as Amazon SES
    /// </summary>
    /// <param name="emails">The emails to send</param>
    /// <returns>Task&lt;List&lt;ApiResponse&lt;Yunit&gt;&gt;&gt;, with one response for each email, in the same order as the emails passed in</returns>
    public async Task<List<ApiResponse<Yunit>>> SendEmailsAsync(IEnumerable<EmailParameters> emails) {
      List<ApiResponse<Yunit>> responses = [];
      using SmtpClient client = new();
      foreach (EmailParameters emailParameters in emails) {
        try {
          // Connects before the first email, and again if the server dropped the connection after a failed one
          if (!client.IsConnected) {
            await client.ConnectAsync(SmtpSettings.Server, SmtpSettings.Port, SmtpSettings.EffectiveSocketOptions());
            await client.AuthenticateAsync(SmtpSettings.UserName, SmtpSettings.Password);
          }
          await client.SendAsync(CreateMailMessage(emailParameters));
          responses.Add(new ApiResponse<Yunit>(ApiResponseStates.Success));
        }
        catch (Exception ex) {
          responses.Add(new ApiResponse<Yunit>(ApiResponseStates.Failure, Message: $"({ex.GetType().Name}) {ex.Message}"));
        }
      }
      if (client.IsConnected) {
        try {
          await client.DisconnectAsync(true);
        }
        catch (Exception) {
          // The emails have all been sent (or not) by now, so there's nothing useful to do if the server doesn't say goodbye nicely
        }
      }
      return responses;
    }

    private async Task<ApiResponse<Yunit>> SendMessageAsync(MimeMessage msg, Func<SmtpClient, Task> connectFunc, bool includeExceptionType = true) {
      try {
        using SmtpClient client = new();
        await connectFunc(client);
        await client.AuthenticateAsync(SmtpSettings.UserName, SmtpSettings.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
        return new ApiResponse<Yunit>(ApiResponseStates.Success);
      }
      catch (Exception ex) {
        string message = includeExceptionType ? $"({ex.GetType().Name}) {ex.Message}" : ex.Message;
        return new ApiResponse<Yunit>(ApiResponseStates.Failure, Message: message);
      }
    }

    private MimeMessage CreateMailMessage(EmailParameters parameters) {
      MimeMessage mm = new() {
        Subject = parameters.Subject,
      };
      mm.From.Add(parameters.From ?? new MailboxAddress(SmtpSettings.FromName, SmtpSettings.FromEmail));
      parameters.Recipients.ForEach(r => mm.To.Add(r));
      parameters.Cc.ForEach(r => mm.Cc.Add(r));
      parameters.Bcc.ForEach(r => mm.Bcc.Add(r));
      if (parameters.ReplyTo != null) {
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