using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using Pixata.Extensions;

namespace Pixata.Email;

/// <summary>
/// Reads the messages in a Microsoft 365 mailbox (or shared mailbox) with Microsoft Graph, logging in with an OAuth access token, such as to
/// take in the invoices that suppliers email to a shared address. Register it with <c>AddHttpClient&lt;PixataGraphMailReader&gt;()</c>, or
/// create one with your own <see cref="HttpClient"/>
/// </summary>
public class PixataGraphMailReader(HttpClient httpClient, GraphEmailSettings? settings = null) {
  /// <summary>
  /// The access token and mailbox to read. The token needs the <c>Mail.Read</c> permission, or <c>Mail.ReadWrite</c> to mark messages as read
  /// or move them. Set this before reading if they weren't passed to the constructor, eg to use a new token once the old one has expired
  /// </summary>
  public GraphEmailSettings Settings { get; set; } = settings ?? new();

  private const string MessageFields = "id,internetMessageId,subject,from,sender,toRecipients,ccRecipients,replyTo,receivedDateTime,body,isRead,hasAttachments,conversationId";

  // The page size Graph is asked for. Bigger pages mean fewer requests, but each message has its body
  private const int PageSize = 50;

  /// <summary>
  /// Gets the messages in a folder, oldest first, so you can deal with them in the order they came in
  /// </summary>
  /// <param name="query">Which folder, and which messages. If omitted, the first 100 messages in the inbox</param>
  /// <param name="cancellationToken">Cancels the request</param>
  /// <returns>The messages. A token that has expired or been revoked, or that doesn't allow reading the mailbox, gives
  /// <see cref="ApiResponseStates.Unauthorised"/>, and a folder that doesn't exist gives <see cref="ApiResponseStates.NotFound"/></returns>
  public async Task<ApiResponse<List<ReceivedEmail>>> GetMessages(GraphMailQuery? query = null, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<List<ReceivedEmail>>(Settings) is { } noToken) {
      return noToken;
    }
    query ??= new();
    List<ReceivedEmail> messages = [];
    string? url = MessagesUrl(query);
    try {
      while (url is not null && messages.Count < query.MaxCount) {
        ApiResponse<JsonNode?> page = await GraphRequests.Send(httpClient, Settings, HttpMethod.Get, url, notFound: true, cancellationToken: cancellationToken,
          configure: request => {
            if (query.BodyAsText) {
              request.Headers.Add("Prefer", "outlook.body-content-type=\"text\"");
            }
          });
        if (!page) {
          return GraphRequests.Failed<List<ReceivedEmail>>(page);
        }
        messages.AddRange(Items(page.Data).Select(Email).Take(query.MaxCount - messages.Count));
        url = page.Data?["@odata.nextLink"]?.GetValue<string>();
      }
      return new(ApiResponseStates.Success, messages);
    }
    catch (Exception ex) {
      return GraphRequests.Caught<List<ReceivedEmail>>(ex);
    }
  }

  /// <summary>
  /// Gets the details of a message's attachments, without their content, which you get with <see cref="OpenAttachment"/>
  /// </summary>
  /// <param name="messageId">The message's <see cref="ReceivedEmail.Id"/></param>
  /// <param name="includeInline">Whether to include the attachments shown in the body, such as images in a signature</param>
  /// <param name="cancellationToken">Cancels the request</param>
  public async Task<ApiResponse<List<ReceivedAttachment>>> GetAttachments(string messageId, bool includeInline = false, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<List<ReceivedAttachment>>(Settings) is { } noToken) {
      return noToken;
    }
    List<ReceivedAttachment> attachments = [];
    // contentBytes is left out, as it would bring the whole of every attachment
    string? url = $"{MessageUrl(messageId)}/attachments?$select=id,name,contentType,size,isInline";
    try {
      while (url is not null) {
        ApiResponse<JsonNode?> page = await GraphRequests.Send(httpClient, Settings, HttpMethod.Get, url, notFound: true, cancellationToken: cancellationToken);
        if (!page) {
          return GraphRequests.Failed<List<ReceivedAttachment>>(page);
        }
        attachments.AddRange(Items(page.Data).Select(Attachment).Where(a => includeInline || !a.IsInline));
        url = page.Data?["@odata.nextLink"]?.GetValue<string>();
      }
      return new(ApiResponseStates.Success, attachments);
    }
    catch (Exception ex) {
      return GraphRequests.Caught<List<ReceivedAttachment>>(ex);
    }
  }

  /// <summary>
  /// Opens an attachment's content as a stream, which is read from Graph as you read it, so even a large attachment isn't held in memory.
  /// Dispose of the stream when you've finished with it
  /// </summary>
  /// <param name="messageId">The message's <see cref="ReceivedEmail.Id"/></param>
  /// <param name="attachmentId">The attachment's <see cref="ReceivedAttachment.Id"/></param>
  /// <param name="cancellationToken">Cancels the request</param>
  public async Task<ApiResponse<Stream>> OpenAttachment(string messageId, string attachmentId, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<Stream>(Settings) is { } noToken) {
      return noToken;
    }
    HttpResponseMessage? response = null;
    try {
      using HttpRequestMessage request = GraphRequests.Request(Settings, HttpMethod.Get, $"{MessageUrl(messageId)}/attachments/{Uri.EscapeDataString(attachmentId)}/$value");
      response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      if (!response.IsSuccessStatusCode) {
        using HttpResponseMessage failed = response;
        return new(GraphRequests.State(response.StatusCode, true), Message: await GraphRequests.ErrorMessage(response));
      }
      Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
      return new(ApiResponseStates.Success, new ResponseStream(response, content));
    }
    catch (Exception ex) {
      response?.Dispose();
      return GraphRequests.Caught<Stream>(ex);
    }
  }

  /// <summary>
  /// Marks a message as read (or unread), so it isn't picked up again by a query with <see cref="GraphMailQuery.UnreadOnly"/>. Needs the
  /// <c>Mail.ReadWrite</c> permission
  /// </summary>
  /// <param name="messageId">The message's <see cref="ReceivedEmail.Id"/></param>
  /// <param name="isRead">False to mark it as unread</param>
  /// <param name="cancellationToken">Cancels the request</param>
  public async Task<ApiResponse<Yunit>> MarkAsRead(string messageId, bool isRead = true, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<Yunit>(Settings) is { } noToken) {
      return noToken;
    }
    try {
      ApiResponse<JsonNode?> response = await GraphRequests.Send(httpClient, Settings, HttpMethod.Patch, MessageUrl(messageId), new JsonObject { ["isRead"] = isRead },
        notFound: true, cancellationToken: cancellationToken);
      return response ? new(ApiResponseStates.Success) : GraphRequests.Failed<Yunit>(response);
    }
    catch (Exception ex) {
      return GraphRequests.Caught<Yunit>(ex);
    }
  }

  /// <summary>
  /// Moves a message to another folder, such as "Processed", so it isn't picked up again. Needs the <c>Mail.ReadWrite</c> permission
  /// </summary>
  /// <param name="messageId">The message's <see cref="ReceivedEmail.Id"/></param>
  /// <param name="destinationFolder">The folder's id (see <see cref="GetFolderId"/>), or a well-known name such as <c>archive</c> or
  /// <c>deleteditems</c></param>
  /// <param name="cancellationToken">Cancels the request</param>
  /// <returns>The message's new id, as a message's id changes when it's moved</returns>
  public async Task<ApiResponse<string>> Move(string messageId, string destinationFolder, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<string>(Settings) is { } noToken) {
      return noToken;
    }
    try {
      ApiResponse<JsonNode?> response = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, $"{MessageUrl(messageId)}/move",
        new JsonObject { ["destinationId"] = destinationFolder }, notFound: true, cancellationToken: cancellationToken);
      return response
        ? new(ApiResponseStates.Success, response.Data?["id"]?.GetValue<string>() ?? "")
        : GraphRequests.Failed<string>(response);
    }
    catch (Exception ex) {
      return GraphRequests.Caught<string>(ex);
    }
  }

  /// <summary>
  /// Gets the id of a folder from its path, such as "Inbox/Processed", to pass to <see cref="Move"/> or <see cref="GraphMailQuery.Folder"/>.
  /// Each part is a folder's display name, and the first can also be a well-known name such as <c>inbox</c>, which works whatever the
  /// mailbox's language
  /// </summary>
  /// <param name="path">The folder's path, with the parts separated by /</param>
  /// <param name="create">Whether to create the folders that don't exist yet, which needs the <c>Mail.ReadWrite</c> permission. Otherwise, a
  /// folder that doesn't exist gives <see cref="ApiResponseStates.NotFound"/></param>
  /// <param name="cancellationToken">Cancels the request</param>
  public async Task<ApiResponse<string>> GetFolderId(string path, bool create = false, CancellationToken cancellationToken = default) {
    if (GraphRequests.NoToken<string>(Settings) is { } noToken) {
      return noToken;
    }
    string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0) {
      return new(ApiResponseStates.Failure, Message: "The folder's path is empty");
    }
    try {
      string? parentId = null;
      foreach (string part in parts) {
        string folders = parentId is null ? "mailFolders" : $"mailFolders/{Uri.EscapeDataString(parentId)}/childFolders";
        string filter = Uri.EscapeDataString($"displayName eq '{part.Replace("'", "''")}'");
        ApiResponse<JsonNode?> found = await GraphRequests.Send(httpClient, Settings, HttpMethod.Get, $"{folders}?$filter={filter}&$select=id",
          notFound: true, cancellationToken: cancellationToken);
        if (!found) {
          return GraphRequests.Failed<string>(found);
        }
        string? id = Items(found.Data).Select(f => f["id"]?.GetValue<string>()).FirstOrDefault();
        if (id is null && parentId is null) {
          // A well-known name, such as inbox, isn't a display name, and Graph says it's malformed rather than missing if it isn't one
          ApiResponse<JsonNode?> wellKnown = await GraphRequests.Send(httpClient, Settings, HttpMethod.Get, $"mailFolders/{Uri.EscapeDataString(part)}?$select=id",
            notFound: true, cancellationToken: cancellationToken);
          id = wellKnown ? wellKnown.Data?["id"]?.GetValue<string>() : null;
        }
        if (id is null) {
          if (!create) {
            return new(ApiResponseStates.NotFound, Message: $"There's no folder called \"{part}\" in {(parentId is null ? "the mailbox" : "its parent")}");
          }
          ApiResponse<JsonNode?> created = await GraphRequests.Send(httpClient, Settings, HttpMethod.Post, folders, new JsonObject { ["displayName"] = part },
            cancellationToken: cancellationToken);
          if (!created) {
            return GraphRequests.Failed<string>(created);
          }
          id = created.Data?["id"]?.GetValue<string>() ?? "";
        }
        parentId = id;
      }
      return new(ApiResponseStates.Success, parentId);
    }
    catch (Exception ex) {
      return GraphRequests.Caught<string>(ex);
    }
  }

  private static string MessageUrl(string messageId) =>
    $"messages/{Uri.EscapeDataString(messageId)}";

  // Oldest first. When the messages are filtered as well as sorted, Graph needs the field they're sorted by to be filtered on first, or it
  // refuses the query as inefficient, so a query for unread messages has a date filter that lets everything through
  private static string MessagesUrl(GraphMailQuery query) {
    string select = query.IncludeHeaders ? $"{MessageFields},internetMessageHeaders" : MessageFields;
    List<string> filters = [];
    if (query.Since is not null || query.UnreadOnly) {
      DateTimeOffset since = query.Since ?? new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
      filters.Add($"receivedDateTime ge {since.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}");
    }
    if (query.UnreadOnly) {
      filters.Add("isRead eq false");
    }
    string url = $"mailFolders/{Uri.EscapeDataString(query.Folder)}/messages?$select={select}&$orderby={Uri.EscapeDataString("receivedDateTime asc")}"
                 + $"&$top={Math.Clamp(query.MaxCount, 1, PageSize)}";
    return filters.Count == 0 ? url : $"{url}&$filter={Uri.EscapeDataString(string.Join(" and ", filters))}";
  }

  private static IEnumerable<JsonNode> Items(JsonNode? page) =>
    page?["value"] is JsonArray items ? items.OfType<JsonNode>() : [];

  private static ReceivedEmail Email(JsonNode message) =>
    new(
      Text(message, "id"),
      Text(message, "internetMessageId"),
      Text(message, "subject"),
      Address(message["from"]),
      Address(message["sender"]),
      Addresses(message["toRecipients"]),
      Addresses(message["ccRecipients"]),
      Addresses(message["replyTo"]),
      DateTimeOffset.TryParse(Text(message, "receivedDateTime"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset received)
        ? received
        : default,
      message["body"]?["content"]?.GetValue<string>() ?? "",
      !string.Equals(message["body"]?["contentType"]?.GetValue<string>(), "text", StringComparison.OrdinalIgnoreCase),
      message["isRead"]?.GetValue<bool>() ?? false,
      message["hasAttachments"]?.GetValue<bool>() ?? false,
      Text(message, "conversationId"),
      message["internetMessageHeaders"] is JsonArray headers
        ? [.. headers.OfType<JsonNode>().Select(h => (Text(h, "name"), Text(h, "value")))]
        : []);

  private static ReceivedAttachment Attachment(JsonNode attachment) =>
    new(
      Text(attachment, "id"),
      Text(attachment, "name"),
      Text(attachment, "contentType"),
      attachment["size"]?.GetValue<long>() ?? 0,
      attachment["isInline"]?.GetValue<bool>() ?? false,
      Text(attachment, "@odata.type") switch {
        "#microsoft.graph.itemAttachment" => ReceivedAttachmentKind.Item,
        "#microsoft.graph.referenceAttachment" => ReceivedAttachmentKind.Reference,
        _ => ReceivedAttachmentKind.File
      });

  private static string Text(JsonNode node, string name) =>
    node[name]?.GetValue<string>() ?? "";

  private static List<MailboxAddress> Addresses(JsonNode? recipients) =>
    recipients is JsonArray array ? [.. array.Select(Address).OfType<MailboxAddress>()] : [];

  // Graph's addresses look like {"emailAddress": {"name": "Jim Spriggs", "address": "jim@spriggs.com"}}. A sender inside the organisation can
  // have an Exchange address (such as /O=EXCHANGELABS/OU=...) rather than an email address, which MimeKit won't take, so that's kept as the name
  private static MailboxAddress? Address(JsonNode? recipient) {
    if (recipient?["emailAddress"] is not JsonNode emailAddress) {
      return null;
    }
    string name = emailAddress["name"]?.GetValue<string>() ?? "";
    string address = emailAddress["address"]?.GetValue<string>() ?? "";
    return MailboxAddress.TryParse(address, out MailboxAddress? parsed)
      ? new(name, parsed.Address)
      : new(string.IsNullOrWhiteSpace(name) ? address : name, "");
  }

  // Keeps the response alive while its content is read, and disposes of it with the stream
  private sealed class ResponseStream(HttpResponseMessage response, Stream content) : Stream {
    public override bool CanRead => content.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => response.Content.Headers.ContentLength ?? throw new NotSupportedException();
    public override long Position { get => content.Position; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => content.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => content.Read(buffer);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => content.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => content.ReadAsync(buffer, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing) {
      if (disposing) {
        content.Dispose();
        response.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}
