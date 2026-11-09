using System;

namespace Pixata.Email;

/// <summary>
/// Which messages <see cref="PixataGraphMailReader.GetMessages"/> gets from a mailbox, and how
/// </summary>
public class GraphMailQuery {
  /// <summary>
  /// The folder to read, as a well-known name (such as <c>inbox</c> or <c>archive</c>) or a folder's id. To read a folder by its path, such as
  /// "Inbox/Invoices", get its id with <see cref="PixataGraphMailReader.GetFolderId"/>
  /// </summary>
  public string Folder { get; set; } = "inbox";

  /// <summary>
  /// Only get messages that haven't been read
  /// </summary>
  public bool UnreadOnly { get; set; }

  /// <summary>
  /// Only get messages received at or after this time
  /// </summary>
  public DateTimeOffset? Since { get; set; }

  /// <summary>
  /// The most messages to get. They are oldest first, so the rest are the newer ones, which you get next time, once you've marked these as
  /// read or moved them
  /// </summary>
  public int MaxCount { get; set; } = 100;

  /// <summary>
  /// Get the body as plain text, which Graph makes from the HTML, rather than as HTML
  /// </summary>
  public bool BodyAsText { get; set; }

  /// <summary>
  /// Get the message's internet headers as well, such as <c>Delivered-To</c>, which says which address a message was delivered to when it
  /// isn't in the To or CC (eg if it was sent to a group, or BCC). Graph only sends these when asked, as they make the response much larger
  /// </summary>
  public bool IncludeHeaders { get; set; }
}
