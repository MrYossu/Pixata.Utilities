using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MimeKit;
using Pixata.Extensions;

namespace Pixata.Email;

/// <summary>
/// Sends emails through a Microsoft 365 mailbox with Microsoft Graph (<c>sendMail</c>), logging in with an OAuth access token. Works where SMTP
/// logins are turned off for the mailbox, and saves the emails in Sent Items. Register it with <c>AddHttpClient&lt;PixataGraphEmailService&gt;()</c>,
/// or create one with your own <see cref="HttpClient"/>
/// </summary>
public class PixataGraphEmailService(HttpClient httpClient, GraphEmailSettings? settings = null) : PixataEmailSenderInterface {
  /// <summary>
  /// The access token and mailbox to send with. Set this before sending if they weren't passed to the constructor, eg when each of your
  /// customers connects their own mailbox, or to use a new token once the old one has expired
  /// </summary>
  public GraphEmailSettings Settings { get; set; } = settings ?? new();

  /// <summary>
  /// Send an email from the mailbox to the email address passed in
  /// </summary>
  /// <param name="email">The recipient email address</param>
  /// <param name="subject">The email subject line</param>
  /// <param name="htmlMessage">An HTML string for the body of the email</param>
  /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;</returns>
  public Task<ApiResponse<Yunit>> SendEmailAsync(string email, string subject, string htmlMessage) =>
    SendEmailAsync(new(subject, htmlMessage, email));

  /// <summary>
  /// Send an email from the mailbox. Allows multiple recipients, CC, BCC and attachments. An email that's too big for Graph's
  /// <c>sendMail</c> (which takes up to 4MB, so about 3MB of attachments) is created as a draft, its attachments are uploaded, and the draft
  /// is sent, which needs the <c>Mail.ReadWrite</c> permission as well as <c>Mail.Send</c>
  /// </summary>
  /// <param name="emailParameters">An EmailParameters object, which contains all the data for the email</param>
  /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;. A token that has expired or been revoked gives <see cref="ApiResponseStates.Unauthorised"/>,
  /// Graph being busy or throttling the app gives <see cref="ApiResponseStates.ServiceUnavailable"/>, and not being able to reach Graph gives
  /// <see cref="ApiResponseStates.HttpFailure"/></returns>
  public async Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters) {
    if (GraphRequests.NoToken<Yunit>(Settings) is { } noToken) {
      return noToken;
    }
    try {
      if (SendMailSize(emailParameters) > MaxSendMailBytes) {
        return await SendAsDraft(emailParameters);
      }
      ApiResponse<JsonNode?> response = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, "sendMail", Request(emailParameters));
      return response
        ? new(ApiResponseStates.Success)
        : new(response.State, Message: TooLarge(response.Message));
    }
    catch (Exception ex) {
      return GraphRequests.Caught<Yunit>(ex);
    }
  }

  /// <summary>
  /// Send several emails from the mailbox, one after the other. Graph takes each email as a separate request, so this is the same as calling
  /// <see cref="SendEmailAsync(EmailParameters)"/> for each one, but lets you swap between this and <see cref="PixataEmailService"/>
  /// </summary>
  /// <param name="emails">The emails to send</param>
  /// <returns>Task&lt;List&lt;ApiResponse&lt;Yunit&gt;&gt;&gt;, with one response for each email, in the same order as the emails passed in</returns>
  public async Task<List<ApiResponse<Yunit>>> SendEmailsAsync(IEnumerable<EmailParameters> emails) {
    List<ApiResponse<Yunit>> responses = [];
    foreach (EmailParameters emailParameters in emails) {
      responses.Add(await SendEmailAsync(emailParameters));
    }
    return responses;
  }

  // Graph's sendMail takes requests of up to 4MB. Anything bigger is sent as a draft, with a little room left for the JSON around the message
  internal const long MaxSendMailBytes = 3_500_000;

  // Graph takes an attachment smaller than this in one request, and a bigger one through an upload session, in chunks
  internal const int UploadSessionThreshold = 3 * 1024 * 1024;

  // Each chunk of an upload must be a multiple of 320KiB, and smaller than 4MB
  internal const int ChunkSize = 12 * 320 * 1024;

  // Roughly how big the sendMail request would be. The attachments are base64 encoded, so they grow by a third
  private static long SendMailSize(EmailParameters parameters) =>
    Encoding.UTF8.GetByteCount(parameters.Body) + parameters.Attachments.Sum(a => (a.Data.Length + 2L) / 3 * 4);

  // Creates the message as a draft, adds the attachments, and sends it. If any of that fails, the draft is deleted, so it isn't left in
  // the mailbox's Drafts folder
  private async Task<ApiResponse<Yunit>> SendAsDraft(EmailParameters parameters) {
    JsonObject message = Message(parameters, false);
    if (!Settings.SaveToSentItems) {
      // PidTagDeleteAfterSubmit, as sending a draft always saves it in Sent Items otherwise
      message["singleValueExtendedProperties"] = new JsonArray(new JsonObject { ["id"] = "Boolean 0x0E01", ["value"] = "true" });
    }
    ApiResponse<JsonNode?> created = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, "messages", message);
    if (!created) {
      return new(created.State, Message: Draft(created.Message));
    }
    string id = created.Data?["id"]?.GetValue<string>() ?? "";
    string messageUrl = $"messages/{Uri.EscapeDataString(id)}";
    ApiResponse<Yunit> result;
    try {
      result = await AddAttachments(messageUrl, parameters);
      if (result) {
        ApiResponse<JsonNode?> sent = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, $"{messageUrl}/send");
        result = sent ? new(ApiResponseStates.Success) : new(sent.State, Message: sent.Message);
      }
    }
    catch (Exception ex) {
      result = GraphRequests.Caught<Yunit>(ex);
    }
    if (!result) {
      await DeleteDraft(messageUrl);
    }
    return result;
  }

  private async Task<ApiResponse<Yunit>> AddAttachments(string messageUrl, EmailParameters parameters) {
    foreach ((string FileName, string MimeType, byte[] Data) attachment in parameters.Attachments) {
      ApiResponse<Yunit> added = attachment.Data.Length < UploadSessionThreshold
        ? await AddAttachment(messageUrl, attachment)
        : await UploadAttachment(messageUrl, attachment);
      if (!added) {
        return new(added.State, Message: $"Couldn't attach {attachment.FileName}: {added.Message}");
      }
    }
    return new(ApiResponseStates.Success);
  }

  private async Task<ApiResponse<Yunit>> AddAttachment(string messageUrl, (string FileName, string MimeType, byte[] Data) attachment) {
    ApiResponse<JsonNode?> response = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, $"{messageUrl}/attachments", Attachment(attachment));
    return response ? new(ApiResponseStates.Success) : GraphRequests.Failed<Yunit>(response);
  }

  // https://learn.microsoft.com/graph/outlook-large-attachments
  private async Task<ApiResponse<Yunit>> UploadAttachment(string messageUrl, (string FileName, string MimeType, byte[] Data) attachment) {
    JsonObject item = new() {
      ["AttachmentItem"] = new JsonObject {
        ["attachmentType"] = "file",
        ["name"] = attachment.FileName,
        ["size"] = attachment.Data.Length,
        ["contentType"] = attachment.MimeType
      }
    };
    ApiResponse<JsonNode?> session = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, $"{messageUrl}/attachments/createUploadSession", item);
    if (!session) {
      return GraphRequests.Failed<Yunit>(session);
    }
    string uploadUrl = session.Data?["uploadUrl"]?.GetValue<string>() ?? "";
    for (int start = 0; start < attachment.Data.Length; start += ChunkSize) {
      int length = Math.Min(ChunkSize, attachment.Data.Length - start);
      // The upload URL is authorised by itself, and Graph refuses a chunk that has the token as well
      using HttpRequestMessage request = new(HttpMethod.Put, uploadUrl) {
        Content = new ByteArrayContent(attachment.Data, start, length)
      };
      request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
      request.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, start + length - 1, attachment.Data.Length);
      using HttpResponseMessage response = await httpClient.SendAsync(request);
      if (!response.IsSuccessStatusCode) {
        return new(GraphRequests.State(response.StatusCode), Message: await GraphRequests.ErrorMessage(response));
      }
    }
    return new(ApiResponseStates.Success);
  }

  private async Task DeleteDraft(string messageUrl) {
    try {
      await GraphRequests.Send(httpClient, Settings, HttpMethod.Delete, messageUrl);
    }
    catch (Exception) {
      // The draft is left behind, but the reason the email wasn't sent matters more than this
    }
  }

  private static string Draft(string message) =>
    message.StartsWith("(403 ")
      ? $"{message}. Sending an email with large attachments creates it as a draft first, which needs the Mail.ReadWrite permission as well as Mail.Send"
      : message;

  private static string TooLarge(string message) =>
    message.StartsWith("(413 ")
      ? $"{message}. Graph's sendMail only takes requests up to 4MB"
      : message;

  // The body of a sendMail request: https://learn.microsoft.com/graph/api/user-sendmail
  private JsonObject Request(EmailParameters parameters) =>
    new() {
      ["message"] = Message(parameters, true),
      ["saveToSentItems"] = Settings.SaveToSentItems
    };

  private static JsonObject Message(EmailParameters parameters, bool withAttachments) {
    JsonObject message = new() {
      ["subject"] = parameters.Subject,
      ["body"] = new JsonObject {
        ["contentType"] = "HTML",
        ["content"] = parameters.Body
      },
      ["toRecipients"] = Recipients(parameters.Recipients)
    };
    if (parameters.Cc.Count > 0) {
      message["ccRecipients"] = Recipients(parameters.Cc);
    }
    if (parameters.Bcc.Count > 0) {
      message["bccRecipients"] = Recipients(parameters.Bcc);
    }
    if (parameters.ReplyTo is not null) {
      message["replyTo"] = Recipients([parameters.ReplyTo]);
    }
    // Without this, the email is from the mailbox. Sending from another address needs "Send As" or "Send on behalf" permission for it
    if (parameters.From is not null) {
      message["from"] = Recipient(parameters.From);
    }
    if (withAttachments && parameters.Attachments.Count > 0) {
      message["attachments"] = new JsonArray([.. parameters.Attachments.Select(Attachment)]);
    }
    return message;
  }

  private static JsonObject Attachment((string FileName, string MimeType, byte[] Data) attachment) =>
    new() {
      ["@odata.type"] = "#microsoft.graph.fileAttachment",
      ["name"] = attachment.FileName,
      ["contentType"] = attachment.MimeType,
      ["contentBytes"] = Convert.ToBase64String(attachment.Data)
    };

  private static JsonArray Recipients(IEnumerable<MailboxAddress> addresses) =>
    new([.. addresses.Select(Recipient)]);

  private static JsonObject Recipient(MailboxAddress address) {
    JsonObject emailAddress = new() {
      ["address"] = address.Address
    };
    if (!string.IsNullOrWhiteSpace(address.Name)) {
      emailAddress["name"] = address.Name;
    }
    return new() {
      ["emailAddress"] = emailAddress
    };
  }
}
