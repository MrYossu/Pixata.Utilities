using System.Collections.Generic;
using System.Linq;
using MimeKit;

namespace Pixata.Email {
  public class EmailParameters {
    // This is used to detect whether the ReplyTo property has been explicitly set. We assume that "jim" is not a tol-level domain!
    private const string Jim = "jim@jim.jim";

    /// <summary>
    /// Create a new EmailParameters object
    /// </summary>
    /// <param name="subject">The subject line of the email</param>
    /// <param name="body">The HTML body of the email</param>
    /// <param name="recipientEmail">The email address of the recipient</param>
    /// <param name="recipientName">The name of the recipient</param>
    public EmailParameters(string subject, string body, string recipientEmail, string recipientName) =>
      SetProperties(subject, body, recipientEmail, recipientName);

    /// <summary>
    /// Create a new EmailParameters object
    /// </summary>
    /// <param name="subject">The subject line of the email</param>
    /// <param name="body">The HTML body of the email</param>
    /// <param name="recipientEmail">The email address of the recipient</param>
    public EmailParameters(string subject, string body, string recipientEmail) =>
      SetProperties(subject, body, new List<string> { recipientEmail });

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subject">The subject line of the email</param>
    /// <param name="body">The HTML body of the email</param>
    /// <param name="recipientEmails">The email addresses of the recipients</param>
    public EmailParameters(string subject, string body, IEnumerable<string> recipientEmails) =>
      SetProperties(subject, body, recipientEmails);

    private void SetProperties(string subject, string body, IEnumerable<string> recipientEmails) {
      Subject = subject;
      Body = body;
      Recipients = recipientEmails.Select(MailboxAddress.Parse).ToList();
      ReplyTo = MailboxAddress.Parse(Jim);
    }

    private void SetProperties(string subject, string body, string recipientEmail, string recipientName = "") {
      Subject = subject;
      Body = body;
      Recipients = new List<MailboxAddress> { new(recipientName, recipientEmail) };
      ReplyTo = MailboxAddress.Parse(Jim);
    }

    /// <summary>
    /// The email address of the sender, represented as a MailboxAddress object (from the MimeKit library)
    /// </summary>
    public MailboxAddress From { get; set; }

    /// <summary>
    /// Gets or sets the subject line of the message
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the main content of the message. This is assumed to be HTML
    /// </summary>
    public string Body { get; set; }

    /// <summary>
    /// Gets or sets the list of recipient email addresses for the message. Each address is represented as a MailboxAddress object (from the MimeKit library)
    /// </summary>
    public List<MailboxAddress> Recipients { get; set; }

    /// <summary>
    /// Gets or sets the email address to which replies should be sent for this message
    /// </summary>
    public MailboxAddress ReplyTo { get; set; }

    /// <summary>
    /// Gets a value indicating whether the <c>ReplyTo</c> address has been explicitly set
    /// </summary>
    public bool IsReplyToSet => ReplyTo.Address != Jim;

    /// <summary>
    /// Gets or sets the collection of file attachments for the email
    /// </summary>
    public List<(string FileName, string MimeType, byte[] Data)> Attachments { get; set; } = new();
  }
}