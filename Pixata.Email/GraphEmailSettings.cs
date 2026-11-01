namespace Pixata.Email;

/// <summary>
/// The settings for sending emails with Microsoft Graph, through a Microsoft 365 mailbox
/// </summary>
public class GraphEmailSettings {
  /// <summary>
  /// An access token for Microsoft Graph with the <c>Mail.Send</c> permission. Getting the token, and getting a new one when it expires (they
  /// last about an hour), is up to the app
  /// </summary>
  public string AccessToken { get; set; } = "";

  /// <summary>
  /// The mailbox to send from, as the user's id or user principal name (usually their email address), for a token the app got for itself
  /// (with the <c>Mail.Send</c> application permission). Leave this empty to send from the signed-in user's own mailbox, for a token got on
  /// their behalf
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
