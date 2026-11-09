using System.Net;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions;

namespace Pixata.Email.Tests;

[TestClass]
public class PixataGraphMailReader_Tests {
  private const string Mailbox = "https://graph.microsoft.com/v1.0/users/invoices@spriggs.com/";

  private static PixataGraphMailReader Reader(ScriptedGraph graph) =>
    new(new HttpClient(graph), new() { AccessToken = "the-token", UserId = "invoices@spriggs.com" });

  private const string TwoMessages = """
    {"value": [
      {"id": "m1", "internetMessageId": "<abc@supplier.com>", "subject": "Invoice 1234",
       "from": {"emailAddress": {"name": "Billy Shears", "address": "billy@shears.com"}},
       "sender": {"emailAddress": {"name": "Billy Shears", "address": "billy@shears.com"}},
       "toRecipients": [{"emailAddress": {"name": "Invoices", "address": "invoices@spriggs.com"}}, {"emailAddress": {"address": "jim@spriggs.com"}}],
       "ccRecipients": [], "replyTo": [{"emailAddress": {"name": "Accounts", "address": "accounts@shears.com"}}],
       "receivedDateTime": "2026-11-09T08:30:00Z", "body": {"contentType": "html", "content": "<p>Please find attached</p>"},
       "isRead": false, "hasAttachments": true, "conversationId": "c1"},
      {"id": "m2", "subject": "Re: statement",
       "from": {"emailAddress": {"name": "Jim Spriggs", "address": "/O=EXCHANGELABS/OU=EXCHANGE ADMINISTRATIVE GROUP/CN=RECIPIENTS/CN=JIM"}},
       "toRecipients": [], "receivedDateTime": "2026-11-09T09:00:00Z", "body": {"contentType": "text", "content": "Thanks"},
       "isRead": true, "hasAttachments": false}
    ]}
    """;

  [TestMethod]
  public async Task GetMessages_ReadsTheInboxOldestFirst() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, TwoMessages) };

    ApiResponse<List<ReceivedEmail>> response = await Reader(graph).GetMessages();

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    ScriptedGraph.Call call = graph.Calls.Single();
    Assert.AreEqual(HttpMethod.Get, call.Method);
    Assert.AreEqual("Bearer the-token", call.Authorization);
    StringAssert.StartsWith(call.Url, Mailbox + "mailFolders/inbox/messages?");
    StringAssert.Contains(call.Url, "$orderby=receivedDateTime asc");
    StringAssert.Contains(call.Url, "$top=50");
    StringAssert.Contains(call.Url, "$select=id,internetMessageId,subject,from,");
    Assert.IsFalse(call.Url.Contains("$filter"));
    Assert.IsFalse(call.Url.Contains("internetMessageHeaders"));
    Assert.IsNull(call.Prefer);

    ReceivedEmail first = response.Data![0];
    Assert.AreEqual("m1", first.Id);
    Assert.AreEqual("<abc@supplier.com>", first.InternetMessageId);
    Assert.AreEqual("Invoice 1234", first.Subject);
    Assert.AreEqual("Billy Shears", first.From!.Name);
    Assert.AreEqual("billy@shears.com", first.From.Address);
    Assert.AreEqual("billy@shears.com", first.Sender!.Address);
    CollectionAssert.AreEqual(new[] { "invoices@spriggs.com", "jim@spriggs.com" }, first.To.Select(a => a.Address).ToArray());
    Assert.AreEqual(0, first.Cc.Count);
    Assert.AreEqual("accounts@shears.com", first.ReplyTo.Single().Address);
    Assert.AreEqual(new DateTimeOffset(2026, 11, 9, 8, 30, 0, TimeSpan.Zero), first.ReceivedAt);
    Assert.AreEqual("<p>Please find attached</p>", first.Body);
    Assert.IsTrue(first.BodyIsHtml);
    Assert.IsFalse(first.IsRead);
    Assert.IsTrue(first.HasAttachments);
    Assert.AreEqual("c1", first.ConversationId);
    Assert.AreEqual(0, first.Headers.Count);

    ReceivedEmail second = response.Data[1];
    Assert.AreEqual("Thanks", second.Body);
    Assert.IsFalse(second.BodyIsHtml);
    Assert.IsTrue(second.IsRead);
    Assert.IsNull(second.Sender);
    Assert.AreEqual("", second.InternetMessageId);
    // An Exchange address isn't an email address, so it's kept as the name
    Assert.AreEqual("Jim Spriggs", second.From!.Name);
    Assert.AreEqual("", second.From.Address);
  }

  [TestMethod]
  public async Task GetMessages_UnreadSinceADate_FiltersOnTheDateFirst() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, """{"value": []}""") };

    await Reader(graph).GetMessages(new() { Folder = "AAMkAD=", UnreadOnly = true, Since = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.FromHours(1)) });

    string url = graph.Calls.Single().Url;
    StringAssert.StartsWith(url, Mailbox + "mailFolders/AAMkAD=/messages?");
    StringAssert.Contains(url, "$filter=receivedDateTime ge 2026-11-01T08:00:00Z and isRead eq false");
  }

  [TestMethod]
  public async Task GetMessages_UnreadOnly_StillFiltersOnTheDate() {
    // Graph refuses a query sorted by a field that isn't filtered on first
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, """{"value": []}""") };

    await Reader(graph).GetMessages(new() { UnreadOnly = true });

    StringAssert.Contains(graph.Calls.Single().Url, "$filter=receivedDateTime ge 1900-01-01T00:00:00Z and isRead eq false");
  }

  [TestMethod]
  public async Task GetMessages_FollowsTheNextLink_UpToMaxCount() {
    const string next = "https://graph.microsoft.com/v1.0/users/invoices@spriggs.com/mailFolders/inbox/messages?$skip=2";
    ScriptedGraph graph = new() {
      Respond = (_, url) => url == next
        ? (HttpStatusCode.OK, """{"value": [{"id": "m3"}, {"id": "m4"}], "@odata.nextLink": "https://example.com/never"}""")
        : (HttpStatusCode.OK, $$"""{"value": [{"id": "m1"}, {"id": "m2"}], "@odata.nextLink": "{{next}}"}""")
    };

    ApiResponse<List<ReceivedEmail>> response = await Reader(graph).GetMessages(new() { MaxCount = 3 });

    CollectionAssert.AreEqual(new[] { "m1", "m2", "m3" }, response.Data!.Select(m => m.Id).ToArray());
    Assert.AreEqual(2, graph.Calls.Count);
    StringAssert.Contains(graph.Calls[0].Url, "$top=3");
    Assert.IsTrue(graph.Calls.All(c => c.Authorization == "Bearer the-token"));
  }

  [TestMethod]
  public async Task GetMessages_AsTextWithHeaders() {
    ScriptedGraph graph = new() {
      Respond = (_, _) => (HttpStatusCode.OK, """{"value": [{"id": "m1", "internetMessageHeaders": [{"name": "Delivered-To", "value": "invoices@spriggs.com"}]}]}""")
    };

    ApiResponse<List<ReceivedEmail>> response = await Reader(graph).GetMessages(new() { BodyAsText = true, IncludeHeaders = true });

    ScriptedGraph.Call call = graph.Calls.Single();
    Assert.AreEqual("outlook.body-content-type=\"text\"", call.Prefer);
    StringAssert.Contains(call.Url, ",internetMessageHeaders");
    Assert.AreEqual(("Delivered-To", "invoices@spriggs.com"), response.Data!.Single().Headers.Single());
  }

  [TestMethod]
  public async Task GetMessages_AFolderThatDoesNotExist_IsNotFound() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.NotFound, """{"error": {"code": "ErrorItemNotFound", "message": "The specified object was not found in the store."}}""") };

    ApiResponse<List<ReceivedEmail>> response = await Reader(graph).GetMessages(new() { Folder = "AAMkAD=" });

    Assert.AreEqual(ApiResponseStates.NotFound, response.State);
    Assert.AreEqual("(404 ErrorItemNotFound) The specified object was not found in the store.", response.Message);
  }

  [TestMethod]
  public async Task GetMessages_AnExpiredToken_IsUnauthorised() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.Unauthorized, """{"error": {"code": "InvalidAuthenticationToken", "message": "Lifetime validation failed, the token is expired."}}""") };

    ApiResponse<List<ReceivedEmail>> response = await Reader(graph).GetMessages();

    Assert.AreEqual(ApiResponseStates.Unauthorised, response.State);
  }

  [TestMethod]
  public async Task WithoutAToken_FailsWithoutCallingGraph() {
    ScriptedGraph graph = new();
    PixataGraphMailReader reader = new(new HttpClient(graph));

    Assert.AreEqual(ApiResponseStates.Failure, (await reader.GetMessages()).State);
    Assert.AreEqual(ApiResponseStates.Failure, (await reader.GetAttachments("m1")).State);
    Assert.AreEqual(ApiResponseStates.Failure, (await reader.OpenAttachment("m1", "a1")).State);
    Assert.AreEqual(ApiResponseStates.Failure, (await reader.MarkAsRead("m1")).State);
    Assert.AreEqual(ApiResponseStates.Failure, (await reader.Move("m1", "archive")).State);
    Assert.AreEqual(ApiResponseStates.Failure, (await reader.GetFolderId("Inbox")).State);
    Assert.AreEqual(0, graph.Calls.Count);
  }

  [TestMethod]
  public async Task CantReachGraph_IsAnHttpFailure() {
    PixataGraphMailReader reader = new(new HttpClient(new ThrowingHandler()), new() { AccessToken = "the-token" });

    ApiResponse<List<ReceivedEmail>> response = await reader.GetMessages();

    Assert.AreEqual(ApiResponseStates.HttpFailure, response.State);
    Assert.AreEqual("(HttpRequestException) No such host is known", response.Message);
  }

  private class ThrowingHandler : HttpMessageHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
      throw new HttpRequestException("No such host is known");
  }

  private const string Attachments = """
    {"value": [
      {"@odata.type": "#microsoft.graph.fileAttachment", "id": "a1", "name": "Invoice 1234.pdf", "contentType": "application/pdf", "size": 52000, "isInline": false},
      {"@odata.type": "#microsoft.graph.fileAttachment", "id": "a2", "name": "logo.png", "contentType": "image/png", "size": 3000, "isInline": true},
      {"@odata.type": "#microsoft.graph.itemAttachment", "id": "a3", "name": "Original order", "contentType": null, "size": 9000, "isInline": false},
      {"@odata.type": "#microsoft.graph.referenceAttachment", "id": "a4", "name": "Statement.xlsx", "contentType": null, "size": 500, "isInline": false}
    ]}
    """;

  [TestMethod]
  public async Task GetAttachments_LeavesOutInlineOnesAndTheirContent() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, Attachments) };

    ApiResponse<List<ReceivedAttachment>> response = await Reader(graph).GetAttachments("AAMk/ab+c=");

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(Mailbox + "messages/AAMk/ab+c=/attachments?$select=id,name,contentType,size,isInline", graph.Calls.Single().Url);
    CollectionAssert.AreEqual(new[] { "a1", "a3", "a4" }, response.Data!.Select(a => a.Id).ToArray());
    Assert.AreEqual(new ReceivedAttachment("a1", "Invoice 1234.pdf", "application/pdf", 52000, false, ReceivedAttachmentKind.File), response.Data[0]);
    Assert.AreEqual(ReceivedAttachmentKind.Item, response.Data[1].Kind);
    Assert.AreEqual("", response.Data[1].ContentType);
    Assert.AreEqual(ReceivedAttachmentKind.Reference, response.Data[2].Kind);
  }

  [TestMethod]
  public async Task GetAttachments_CanIncludeInlineOnes() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, Attachments) };

    ApiResponse<List<ReceivedAttachment>> response = await Reader(graph).GetAttachments("m1", includeInline: true);

    Assert.AreEqual(4, response.Data!.Count);
    Assert.IsTrue(response.Data[1].IsInline);
  }

  [TestMethod]
  public async Task GetAttachments_EscapesTheMessageId() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.OK, Attachments) };
    HttpClient client = new(new RawUrlHandler(graph));
    PixataGraphMailReader reader = new(client, new() { AccessToken = "the-token" });

    await reader.GetAttachments("AAMk/ab+c=");

    StringAssert.Contains(RawUrlHandler.LastUrl, "messages/AAMk%2Fab%2Bc%3D/attachments");
  }

  private class RawUrlHandler(HttpMessageHandler inner) : DelegatingHandler(inner) {
    public static string LastUrl { get; private set; } = "";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
      LastUrl = request.RequestUri!.AbsoluteUri;
      return base.SendAsync(request, cancellationToken);
    }
  }

  [TestMethod]
  public async Task OpenAttachment_StreamsTheContent() {
    byte[] pdf = [.. Enumerable.Range(0, 200_000).Select(i => (byte)(i % 256))];
    ScriptedGraph graph = new() { Content = pdf };

    ApiResponse<Stream> response = await Reader(graph).OpenAttachment("m1", "a1");

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(Mailbox + "messages/m1/attachments/a1/$value", graph.Calls.Single().Url);
    Assert.AreEqual("Bearer the-token", graph.Calls.Single().Authorization);
    using MemoryStream copy = new();
    await using (Stream stream = response.Data!) {
      await stream.CopyToAsync(copy);
    }
    CollectionAssert.AreEqual(pdf, copy.ToArray());
    Assert.IsTrue(graph.ContentStreamDisposed);
  }

  [TestMethod]
  public async Task OpenAttachment_ThatHasGone_IsNotFound() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.NotFound, """{"error": {"code": "ErrorItemNotFound", "message": "Not found"}}""") };

    ApiResponse<Stream> response = await Reader(graph).OpenAttachment("m1", "a1");

    Assert.AreEqual(ApiResponseStates.NotFound, response.State);
    Assert.IsNull(response.Data);
  }

  [TestMethod]
  public async Task MarkAsRead_PatchesTheMessage() {
    ScriptedGraph graph = new();

    ApiResponse<Yunit> read = await Reader(graph).MarkAsRead("m1");
    ApiResponse<Yunit> unread = await Reader(graph).MarkAsRead("m1", false);

    Assert.AreEqual(ApiResponseStates.Success, read.State, read.Message);
    Assert.AreEqual(ApiResponseStates.Success, unread.State, unread.Message);
    Assert.AreEqual(HttpMethod.Patch, graph.Calls[0].Method);
    Assert.AreEqual(Mailbox + "messages/m1", graph.Calls[0].Url);
    Assert.AreEqual(true, JsonNode.Parse(graph.Calls[0].Text)!["isRead"]!.GetValue<bool>());
    Assert.AreEqual(false, JsonNode.Parse(graph.Calls[1].Text)!["isRead"]!.GetValue<bool>());
  }

  [TestMethod]
  public async Task MarkAsRead_WithoutReadWrite_IsUnauthorised() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.Forbidden, """{"error": {"code": "ErrorAccessDenied", "message": "Access is denied."}}""") };

    ApiResponse<Yunit> response = await Reader(graph).MarkAsRead("m1");

    Assert.AreEqual(ApiResponseStates.Unauthorised, response.State);
  }

  [TestMethod]
  public async Task Move_ReturnsTheNewId() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.Created, """{"id": "m1-moved"}""") };

    ApiResponse<string> response = await Reader(graph).Move("m1", "folder-9");

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual("m1-moved", response.Data);
    Assert.AreEqual(HttpMethod.Post, graph.Calls.Single().Method);
    Assert.AreEqual(Mailbox + "messages/m1/move", graph.Calls.Single().Url);
    Assert.AreEqual("folder-9", JsonNode.Parse(graph.Calls.Single().Text)!["destinationId"]!.GetValue<string>());
  }

  [TestMethod]
  public async Task GetFolderId_FollowsThePath() {
    ScriptedGraph graph = new() {
      Respond = (_, url) => url switch {
        _ when url.StartsWith(Mailbox + "mailFolders?$filter=displayName eq 'Inbox'") => (HttpStatusCode.OK, """{"value": [{"id": "inbox-id"}]}"""),
        _ when url.StartsWith(Mailbox + "mailFolders/inbox-id/childFolders?$filter=displayName eq 'Jim''s invoices'") => (HttpStatusCode.OK, """{"value": [{"id": "jims-id"}]}"""),
        _ => (HttpStatusCode.BadRequest, "")
      }
    };

    ApiResponse<string> response = await Reader(graph).GetFolderId(" Inbox / Jim's invoices ");

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual("jims-id", response.Data);
    Assert.AreEqual(2, graph.Calls.Count);
  }

  [TestMethod]
  public async Task GetFolderId_TakesAWellKnownName() {
    ScriptedGraph graph = new() {
      Respond = (_, url) => url.StartsWith(Mailbox + "mailFolders?")
        ? (HttpStatusCode.OK, """{"value": []}""")
        : (HttpStatusCode.OK, """{"id": "inbox-id"}""")
    };

    ApiResponse<string> response = await Reader(graph).GetFolderId("inbox");

    Assert.AreEqual("inbox-id", response.Data);
    Assert.AreEqual(Mailbox + "mailFolders/inbox?$select=id", graph.Calls[1].Url);
  }

  [TestMethod]
  public async Task GetFolderId_ThatDoesNotExist_IsNotFound() {
    ScriptedGraph graph = new() {
      Respond = (_, url) => url.Contains("$filter")
        ? (HttpStatusCode.OK, """{"value": []}""")
        : (HttpStatusCode.BadRequest, """{"error": {"code": "ErrorInvalidIdMalformed", "message": "Id is malformed."}}""")
    };

    ApiResponse<string> response = await Reader(graph).GetFolderId("Processed");

    Assert.AreEqual(ApiResponseStates.NotFound, response.State);
    Assert.AreEqual("There's no folder called \"Processed\" in the mailbox", response.Message);
    Assert.IsFalse(graph.Calls.Any(c => c.Method == HttpMethod.Post));
  }

  [TestMethod]
  public async Task GetFolderId_CanCreateTheFolders() {
    ScriptedGraph graph = new() {
      Respond = (method, url) => (method.Method, url) switch {
        ("GET", _) when url.StartsWith(Mailbox + "mailFolders?") => (HttpStatusCode.OK, """{"value": [{"id": "inbox-id"}]}"""),
        ("GET", _) => (HttpStatusCode.OK, """{"value": []}"""),
        ("POST", Mailbox + "mailFolders/inbox-id/childFolders") => (HttpStatusCode.Created, """{"id": "processed-id"}"""),
        _ => (HttpStatusCode.BadRequest, "")
      }
    };

    ApiResponse<string> response = await Reader(graph).GetFolderId("Inbox/Processed", create: true);

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual("processed-id", response.Data);
    Assert.AreEqual("Processed", JsonNode.Parse(graph.Calls.Single(c => c.Method == HttpMethod.Post).Text)!["displayName"]!.GetValue<string>());
  }
}
