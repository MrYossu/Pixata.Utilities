using System.Collections.Generic;
using System.Linq;
using MimeKit;

namespace Pixata.Email {
  public class EmailParameters {
    private const string Jim = "jim@jim.jim";

    public EmailParameters(string subject, string body, string recipientEmail, string recipientName) =>
      SetProperties(subject, body, recipientEmail, recipientName);

    public EmailParameters(string subject, string body, string recipientEmail) =>
      SetProperties(subject, body, new List<string> { recipientEmail });

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

    public string Subject { get; set; }
    public string Body { get; set; }
    public List<MailboxAddress> Recipients { get; set; }
    public MailboxAddress ReplyTo { get; set; }
    public bool IsReplyToSet => ReplyTo.Address != Jim;
    public List<(string FileName, string MimeType, byte[] Data)> Attachments { get; set; } = new();
  }
}