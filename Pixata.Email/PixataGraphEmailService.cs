using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
  /// Send an email from the mailbox. Allows multiple recipients, CC, BCC and attachments
  /// </summary>
  /// <param name="emailParameters">An EmailParameters object, which contains all the data for the email</param>
  /// <returns>Task&lt;ApiResponse&lt;Yunit&gt;&gt;. A token that has expired or been revoked gives <see cref="ApiResponseStates.Unauthorised"/>,
  /// Graph being busy or throttling the app gives <see cref="ApiResponseStates.ServiceUnavailable"/>, and not being able to reach Graph gives
  /// <see cref="ApiResponseStates.HttpFailure"/></returns>
  public async Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters) {
    if (string.IsNullOrWhiteSpace(Settings.AccessToken)) {
      return new(ApiResponseStates.Failure, Message: $"{nameof(GraphEmailSettings)}.{nameof(GraphEmailSettings.AccessToken)} hasn't been set");
    }
    try {
      using HttpRequestMessage request = new(HttpMethod.Post, SendMailUrl()) {
        Content = new StringContent(Request(emailParameters).ToJsonString(), Encoding.UTF8, "application/json")
      };
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.AccessToken);
      using HttpResponseMessage response = await httpClient.SendAsync(request);
      return response.IsSuccessStatusCode
        ? new(ApiResponseStates.Success)
        : new(State(response.StatusCode), Message: await ErrorMessage(response));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
      // TaskCanceledException is what HttpClient throws when it times out
      return new(ApiResponseStates.HttpFailure, Message: $"({ex.GetType().Name}) {ex.Message}");
    }
    catch (Exception ex) {
      return new(ApiResponseStates.Failure, Message: $"({ex.GetType().Name}) {ex.Message}");
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

  private Uri SendMailUrl() {
    string mailbox = string.IsNullOrWhiteSpace(Settings.UserId) ? "me" : $"users/{Uri.EscapeDataString(Settings.UserId)}";
    string baseUrl = Settings.BaseUrl.EndsWith('/') ? Settings.BaseUrl : $"{Settings.BaseUrl}/";
    return new($"{baseUrl}{mailbox}/sendMail");
  }

  // The body of a sendMail request: https://learn.microsoft.com/graph/api/user-sendmail
  private JsonObject Request(EmailParameters parameters) {
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
    if (parameters.Attachments.Count > 0) {
      message["attachments"] = new JsonArray([
        .. parameters.Attachments.Select(a => new JsonObject {
          ["@odata.type"] = "#microsoft.graph.fileAttachment",
          ["name"] = a.FileName,
          ["contentType"] = a.MimeType,
          ["contentBytes"] = Convert.ToBase64String(a.Data)
        })
      ]);
    }
    return new() {
      ["message"] = message,
      ["saveToSentItems"] = Settings.SaveToSentItems
    };
  }

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

  private static ApiResponseStates State(HttpStatusCode status) =>
    status switch {
      HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ApiResponseStates.Unauthorised,
      HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable => ApiResponseStates.ServiceUnavailable,
      _ => ApiResponseStates.Failure
    };

  // Graph's errors look like {"error": {"code": "ErrorAccessDenied", "message": "Access is denied..."}}
  private static async Task<string> ErrorMessage(HttpResponseMessage response) {
    string body = await response.Content.ReadAsStringAsync();
    string message = $"({(int)response.StatusCode} {response.ReasonPhrase}) {body}";
    try {
      if (JsonNode.Parse(body)?["error"] is JsonObject error) {
        message = $"({(int)response.StatusCode} {error["code"]?.GetValue<string>()}) {error["message"]?.GetValue<string>()}";
      }
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
      // Not Graph's usual error, so the body is used as it is
    }
    if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge) {
      message += ". Graph's sendMail only takes requests up to 4MB, which is about 3MB of attachments";
    }
    return message;
  }
}
