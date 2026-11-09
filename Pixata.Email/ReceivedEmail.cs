using System;
using System.Collections.Generic;
using MimeKit;

namespace Pixata.Email;

/// <summary>
/// A message read from a mailbox by <see cref="PixataGraphMailReader"/>
/// </summary>
/// <param name="Id">Graph's id for the message, which you pass to the reader's other methods. It changes when the message is moved</param>
/// <param name="InternetMessageId">The message's Message-ID header, which stays the same wherever the message is</param>
/// <param name="Subject">The subject line</param>
/// <param name="From">Who the message is from</param>
/// <param name="Sender">Who sent it, which is different from <paramref name="From"/> when someone sends on another's behalf</param>
/// <param name="To">The To recipients</param>
/// <param name="Cc">The CC recipients</param>
/// <param name="ReplyTo">The addresses replies should go to, if the sender set any</param>
/// <param name="ReceivedAt">When the message arrived in the mailbox</param>
/// <param name="Body">The body, as HTML, or as text if <see cref="GraphMailQuery.BodyAsText"/> was set</param>
/// <param name="BodyIsHtml">Whether <paramref name="Body"/> is HTML</param>
/// <param name="IsRead">Whether the message has been read</param>
/// <param name="HasAttachments">Whether it has attachments, not counting inline images</param>
/// <param name="ConversationId">Graph's id for the conversation the message is in</param>
/// <param name="Headers">The internet headers, if <see cref="GraphMailQuery.IncludeHeaders"/> was set, otherwise empty</param>
public record ReceivedEmail(
  string Id,
  string InternetMessageId,
  string Subject,
  MailboxAddress? From,
  MailboxAddress? Sender,
  List<MailboxAddress> To,
  List<MailboxAddress> Cc,
  List<MailboxAddress> ReplyTo,
  DateTimeOffset ReceivedAt,
  string Body,
  bool BodyIsHtml,
  bool IsRead,
  bool HasAttachments,
  string ConversationId,
  List<(string Name, string Value)> Headers);

/// <summary>
/// An attachment on a message read by <see cref="PixataGraphMailReader"/>. Get its content with
/// <see cref="PixataGraphMailReader.OpenAttachment"/>
/// </summary>
/// <param name="Id">Graph's id for the attachment</param>
/// <param name="Name">The file name</param>
/// <param name="ContentType">The MIME type, such as <c>application/pdf</c></param>
/// <param name="Size">The size in bytes, roughly. Graph's figure includes some of the attachment's details, so it's a little more than
/// the file's</param>
/// <param name="IsInline">Whether it's shown in the body, such as an image in a signature</param>
/// <param name="Kind">What it is. Most are files. An attached email is an item, and a link to a file in OneDrive or SharePoint is a
/// reference, which has no content</param>
public record ReceivedAttachment(string Id, string Name, string ContentType, long Size, bool IsInline, ReceivedAttachmentKind Kind);

/// <summary>
/// What a <see cref="ReceivedAttachment"/> is
/// </summary>
public enum ReceivedAttachmentKind {
  /// <summary>
  /// A file, such as a PDF
  /// </summary>
  File,

  /// <summary>
  /// An email, contact or event attached to the message. Its content is the item in MIME format, eg a .eml file for an email
  /// </summary>
  Item,

  /// <summary>
  /// A link to a file in OneDrive or SharePoint, which has no content to get
  /// </summary>
  Reference
}
