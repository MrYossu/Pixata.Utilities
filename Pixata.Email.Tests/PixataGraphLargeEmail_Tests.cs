using System.Net;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions;

namespace Pixata.Email.Tests;

[TestClass]
public class PixataGraphLargeEmail_Tests {
  private const string Mailbox = "https://graph.microsoft.com/v1.0/me/";
  private const string UploadUrl = "https://outlook.office.com/api/v2.0/Users('x')/Messages('draft-1')/AttachmentSessions('s1')?authtoken=abc";

  // Graph as it answers a draft being made, attachments added, an upload session started and the draft sent
  private static ScriptedGraph Graph() =>
    new() {
      Respond = (method, url) => (method.Method, url) switch {
        ("POST", Mailbox + "messages") => (HttpStatusCode.Created, """{"id": "draft-1"}"""),
        ("POST", Mailbox + "messages/draft-1/attachments") => (HttpStatusCode.Created, """{"id": "a1"}"""),
        ("POST", Mailbox + "messages/draft-1/attachments/createUploadSession") => (HttpStatusCode.Created, $$"""{"uploadUrl": "{{UploadUrl}}", "nextExpectedRanges": ["0-"]}"""),
        ("PUT", _) => (HttpStatusCode.OK, """{"nextExpectedRanges": []}"""),
        ("POST", Mailbox + "messages/draft-1/send") => (HttpStatusCode.Accepted, ""),
        _ => (HttpStatusCode.BadRequest, $"Unexpected {method} {url}")
      }
    };

  private static PixataGraphEmailService Service(ScriptedGraph graph, bool saveToSentItems = true) =>
    new(new HttpClient(graph), new() { AccessToken = "the-token", SaveToSentItems = saveToSentItems });

  private static byte[] File(int size) =>
    [.. Enumerable.Range(0, size).Select(i => (byte)(i % 251))];

  private static EmailParameters Email(params (string Name, byte[] Data)[] files) {
    EmailParameters email = new("Invoice 1234, approved", "<p>Attached</p>", "finance@spriggs.com");
    email.Attachments.AddRange(files.Select(f => (f.Name, "application/pdf", f.Data)));
    return email;
  }

  [TestMethod]
  public async Task AttachmentsUnder3MB_StillGoInOneSendMail() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.Accepted, "") };

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("a.pdf", File(1_000_000)), ("b.pdf", File(1_500_000))));

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(Mailbox + "sendMail", graph.Calls.Single().Url);
  }

  [TestMethod]
  public async Task LargeAttachments_AreSentAsADraft() {
    ScriptedGraph graph = Graph();
    byte[] large = File(9_000_000);
    byte[] small = File(100_000);

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("scan.pdf", large), ("cover.pdf", small)));

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    List<ScriptedGraph.Call> calls = graph.Calls;
    // The draft is made without the attachments, so that the request is small
    Assert.AreEqual(Mailbox + "messages", calls[0].Url);
    JsonNode draft = JsonNode.Parse(calls[0].Text)!;
    Assert.AreEqual("Invoice 1234, approved", draft["subject"]!.GetValue<string>());
    Assert.AreEqual("finance@spriggs.com", draft["toRecipients"]![0]!["emailAddress"]!["address"]!.GetValue<string>());
    Assert.IsNull(draft["attachments"]);
    Assert.IsNull(draft["singleValueExtendedProperties"]);
    // The large one goes through an upload session
    Assert.AreEqual(Mailbox + "messages/draft-1/attachments/createUploadSession", calls[1].Url);
    JsonNode item = JsonNode.Parse(calls[1].Text)!["AttachmentItem"]!;
    Assert.AreEqual("file", item["attachmentType"]!.GetValue<string>());
    Assert.AreEqual("scan.pdf", item["name"]!.GetValue<string>());
    Assert.AreEqual(9_000_000, item["size"]!.GetValue<int>());
    Assert.AreEqual("application/pdf", item["contentType"]!.GetValue<string>());
    List<ScriptedGraph.Call> chunks = [.. calls.Where(c => c.Method == HttpMethod.Put)];
    Assert.AreEqual(3, chunks.Count);
    Assert.IsTrue(chunks.All(c => c.Url == Uri.UnescapeDataString(UploadUrl)));
    // The upload URL is authorised by itself, and Graph refuses chunks with the token as well
    Assert.IsTrue(chunks.All(c => c.Authorization is null));
    Assert.AreEqual("bytes 0-3932159/9000000", chunks[0].ContentRange);
    Assert.AreEqual("bytes 3932160-7864319/9000000", chunks[1].ContentRange);
    Assert.AreEqual("bytes 7864320-8999999/9000000", chunks[2].ContentRange);
    CollectionAssert.AreEqual(large, chunks.SelectMany(c => c.Body).ToArray());
    // The small one is added in one request
    ScriptedGraph.Call smallCall = calls.Single(c => c.Url == Mailbox + "messages/draft-1/attachments");
    JsonNode attachment = JsonNode.Parse(smallCall.Text)!;
    Assert.AreEqual("cover.pdf", attachment["name"]!.GetValue<string>());
    Assert.AreEqual(Convert.ToBase64String(small), attachment["contentBytes"]!.GetValue<string>());
    // Then the draft is sent, with the token
    Assert.AreEqual(Mailbox + "messages/draft-1/send", calls[^1].Url);
    Assert.AreEqual("Bearer the-token", calls[^1].Authorization);
    Assert.IsFalse(calls.Any(c => c.Method == HttpMethod.Delete));
  }

  [TestMethod]
  public async Task ManySmallAttachments_AddingUpToTooMuch_AreAddedOneByOne() {
    ScriptedGraph graph = Graph();

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("a.pdf", File(2_000_000)), ("b.pdf", File(2_000_000))));

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(2, graph.Calls.Count(c => c.Url == Mailbox + "messages/draft-1/attachments"));
    Assert.IsFalse(graph.Calls.Any(c => c.Url.EndsWith("createUploadSession")));
  }

  [TestMethod]
  public async Task NotSavingToSentItems_MarksTheDraftToBeDeletedOnceSent() {
    ScriptedGraph graph = Graph();

    await Service(graph, saveToSentItems: false).SendEmailAsync(Email(("scan.pdf", File(5_000_000))));

    JsonNode property = JsonNode.Parse(graph.Calls[0].Text)!["singleValueExtendedProperties"]![0]!;
    Assert.AreEqual("Boolean 0x0E01", property["id"]!.GetValue<string>());
    Assert.AreEqual("true", property["value"]!.GetValue<string>());
  }

  [TestMethod]
  public async Task AFailedUpload_DeletesTheDraft() {
    ScriptedGraph graph = Graph();
    Func<HttpMethod, string, (HttpStatusCode, string)> normal = graph.Respond;
    graph.Respond = (method, url) => method == HttpMethod.Put
      ? (HttpStatusCode.ServiceUnavailable, """{"error": {"code": "ServiceUnavailable", "message": "Try again later"}}""")
      : method == HttpMethod.Delete ? (HttpStatusCode.NoContent, "") : normal(method, url);

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("scan.pdf", File(5_000_000))));

    Assert.AreEqual(ApiResponseStates.ServiceUnavailable, response.State);
    Assert.AreEqual("Couldn't attach scan.pdf: (503 ServiceUnavailable) Try again later", response.Message);
    Assert.AreEqual(Mailbox + "messages/draft-1", graph.Calls.Single(c => c.Method == HttpMethod.Delete).Url);
    Assert.IsFalse(graph.Calls.Any(c => c.Url.EndsWith("/send")));
  }

  [TestMethod]
  public async Task AFailedSend_DeletesTheDraft() {
    ScriptedGraph graph = Graph();
    Func<HttpMethod, string, (HttpStatusCode, string)> normal = graph.Respond;
    graph.Respond = (method, url) => url.EndsWith("/send")
      ? (HttpStatusCode.BadRequest, """{"error": {"code": "ErrorMessageSizeExceeded", "message": "The message exceeds the maximum supported size."}}""")
      : method == HttpMethod.Delete ? (HttpStatusCode.NoContent, "") : normal(method, url);

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("scan.pdf", File(5_000_000))));

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    StringAssert.Contains(response.Message, "ErrorMessageSizeExceeded");
    Assert.AreEqual(1, graph.Calls.Count(c => c.Method == HttpMethod.Delete));
  }

  [TestMethod]
  public async Task WithoutMailReadWrite_SaysThatIsWhatIsNeeded() {
    ScriptedGraph graph = new() { Respond = (_, _) => (HttpStatusCode.Forbidden, """{"error": {"code": "ErrorAccessDenied", "message": "Access is denied."}}""") };

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email(("scan.pdf", File(5_000_000))));

    Assert.AreEqual(ApiResponseStates.Unauthorised, response.State);
    StringAssert.StartsWith(response.Message, "(403 ErrorAccessDenied) Access is denied.");
    StringAssert.Contains(response.Message, "Mail.ReadWrite");
    // Nothing was made, so there's nothing to delete
    Assert.AreEqual(1, graph.Calls.Count);
  }
}
