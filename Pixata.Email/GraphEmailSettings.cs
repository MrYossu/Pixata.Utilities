namespace Pixata.Email;

/// <summary>
/// The settings for sending emails with Microsoft Graph, through a Microsoft 365 mailbox
/// </summary>
public class GraphEmailSettings {
  /// <summary>
  /// An access token for Microsoft Graph with the <c>Mail.Send</c> permission to send (and <c>Mail.ReadWrite</c> for emails with more than
  /// about 3MB of attachments), or <c>Mail.Read</c> to read a mailbox. Getting the token, and getting a new one when it expires (they last
  /// about an hour), is up to the app
  /// </summary>
  public string AccessToken { get; set; } = "";

  /// <summary>
  /// The mailbox to send from or read, as the user's id or user principal name (usually their email address), for a token the app got for
  /// itself (with application permissions), or a shared mailbox. Leave this empty to use the signed-in user's own mailbox, for a token got
  /// on their behalf
  /// </summary>
  public string UserId { get; set; } = "";

  /// <summary>
  /// Whether a copy of each email is saved in the mailbox's Sent Items folder
  /// </summary>
  public bool SaveToSentItems { get; set; } = true;

  /// <summary>
  /// The address of Microsoft Graph. Only change this for a national cloud, eg <c>https://graph.microsoft.us/v1.0/</c>
  /// </summary>
  public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0/";
}
