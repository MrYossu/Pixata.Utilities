using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Pixata.Extensions;

namespace Pixata.Email.Tests;

[TestClass]
public class PixataGraphEmailService_Tests {
  // Answers every request with the same response, and keeps the requests (with their bodies, which are gone once the request is disposed)
  private class FakeGraph(HttpStatusCode status = HttpStatusCode.Accepted, string body = "") : HttpMessageHandler {
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];
    public Exception? Throw { get; init; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
      Requests.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
      if (Throw is not null) {
        throw Throw;
      }
      return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
  }

  private static PixataGraphEmailService Service(FakeGraph graph, string userId = "", bool saveToSentItems = true) =>
    new(new HttpClient(graph), new() { AccessToken = "the-token", UserId = userId, SaveToSentItems = saveToSentItems });

  private static EmailParameters Email() =>
    new("Your invoice", "<p>Hello</p>", "billy@shears.com", "Billy Shears");

  [TestMethod]
  public async Task SendsTheEmailToSendMail() {
    FakeGraph graph = new();

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    (HttpRequestMessage request, string body) = graph.Requests.Single();
    Assert.AreEqual(HttpMethod.Post, request.Method);
    Assert.AreEqual("https://graph.microsoft.com/v1.0/me/sendMail", request.RequestUri!.ToString());
    Assert.AreEqual("Bearer the-token", request.Headers.Authorization!.ToString());
    Assert.AreEqual("application/json", request.Content!.Headers.ContentType!.MediaType);
    JsonNode json = JsonNode.Parse(body)!;
    Assert.AreEqual(true, json["saveToSentItems"]!.GetValue<bool>());
    JsonNode message = json["message"]!;
    Assert.AreEqual("Your invoice", message["subject"]!.GetValue<string>());
    Assert.AreEqual("HTML", message["body"]!["contentType"]!.GetValue<string>());
    Assert.AreEqual("<p>Hello</p>", message["body"]!["content"]!.GetValue<string>());
    Assert.AreEqual("billy@shears.com", message["toRecipients"]![0]!["emailAddress"]!["address"]!.GetValue<string>());
    Assert.AreEqual("Billy Shears", message["toRecipients"]![0]!["emailAddress"]!["name"]!.GetValue<string>());
    // Only what was given is sent, so that Graph's defaults apply to the rest
    Assert.IsNull(message["ccRecipients"]);
    Assert.IsNull(message["bccRecipients"]);
    Assert.IsNull(message["replyTo"]);
    Assert.IsNull(message["from"]);
    Assert.IsNull(message["attachments"]);
  }

  [TestMethod]
  public async Task SendsCcBccReplyToFromAndAttachments() {
    FakeGraph graph = new();
    EmailParameters email = Email().AddCc("accounts@shears.com").AddBcc("jim@spriggs.com", "copy@spriggs.com");
    email.ReplyTo = new("Jim's office", "office@spriggs.com");
    email.From = new("Spriggs Accounts", "accounts@spriggs.com");
    email.Attachments.Add(("Invoice 1.pdf", "application/pdf", [1, 2, 3]));

    ApiResponse<Yunit> response = await Service(graph, saveToSentItems: false).SendEmailAsync(email);

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    JsonNode json = JsonNode.Parse(graph.Requests.Single().Body)!;
    Assert.AreEqual(false, json["saveToSentItems"]!.GetValue<bool>());
    JsonNode message = json["message"]!;
    Assert.AreEqual("accounts@shears.com", message["ccRecipients"]![0]!["emailAddress"]!["address"]!.GetValue<string>());
    Assert.IsNull(message["ccRecipients"]![0]!["emailAddress"]!["name"]);
    Assert.AreEqual(2, message["bccRecipients"]!.AsArray().Count);
    Assert.AreEqual("copy@spriggs.com", message["bccRecipients"]![1]!["emailAddress"]!["address"]!.GetValue<string>());
    Assert.AreEqual("office@spriggs.com", message["replyTo"]![0]!["emailAddress"]!["address"]!.GetValue<string>());
    Assert.AreEqual("Spriggs Accounts", message["from"]!["emailAddress"]!["name"]!.GetValue<string>());
    JsonNode attachment = message["attachments"]![0]!;
    Assert.AreEqual("#microsoft.graph.fileAttachment", attachment["@odata.type"]!.GetValue<string>());
    Assert.AreEqual("Invoice 1.pdf", attachment["name"]!.GetValue<string>());
    Assert.AreEqual("application/pdf", attachment["contentType"]!.GetValue<string>());
    Assert.AreEqual(Convert.ToBase64String([1, 2, 3]), attachment["contentBytes"]!.GetValue<string>());
  }

  [TestMethod]
  public async Task WithAUserId_SendsFromThatMailbox() {
    FakeGraph graph = new();

    await Service(graph, userId: "accounts@spriggs.com").SendEmailAsync(Email());

    Assert.AreEqual("https://graph.microsoft.com/v1.0/users/accounts%40spriggs.com/sendMail", graph.Requests.Single().Request.RequestUri!.AbsoluteUri);
  }

  [TestMethod]
  public async Task WithoutAToken_FailsWithoutCallingGraph() {
    FakeGraph graph = new();
    PixataGraphEmailService service = new(new HttpClient(graph));

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    StringAssert.Contains(response.Message, "AccessToken");
    Assert.AreEqual(0, graph.Requests.Count);
  }

  [TestMethod]
  public async Task AnExpiredToken_IsUnauthorisedWithGraphsError() {
    FakeGraph graph = new(HttpStatusCode.Unauthorized, """{"error": {"code": "InvalidAuthenticationToken", "message": "Lifetime validation failed, the token is expired."}}""");

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Unauthorised, response.State);
    Assert.AreEqual("(401 InvalidAuthenticationToken) Lifetime validation failed, the token is expired.", response.Message);
  }

  [TestMethod]
  [DataRow(HttpStatusCode.TooManyRequests, ApiResponseStates.ServiceUnavailable)]
  [DataRow(HttpStatusCode.ServiceUnavailable, ApiResponseStates.ServiceUnavailable)]
  [DataRow(HttpStatusCode.Forbidden, ApiResponseStates.Unauthorised)]
  [DataRow(HttpStatusCode.BadRequest, ApiResponseStates.Failure)]
  public async Task ErrorsHaveTheMatchingState(HttpStatusCode status, ApiResponseStates expected) {
    FakeGraph graph = new(status, "Not JSON");

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email());

    Assert.AreEqual(expected, response.State);
    StringAssert.Contains(response.Message, $"({(int)status} ");
    StringAssert.EndsWith(response.Message, "Not JSON");
  }

  [TestMethod]
  public async Task TooLarge_SaysWhatTheLimitIs() {
    FakeGraph graph = new(HttpStatusCode.RequestEntityTooLarge, """{"error": {"code": "RequestBodyTooLarge", "message": "The request is too large."}}""");

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    StringAssert.Contains(response.Message, "up to 4MB");
  }

  [TestMethod]
  public async Task CantReachGraph_IsAnHttpFailure() {
    FakeGraph graph = new() { Throw = new HttpRequestException("No such host is known") };

    ApiResponse<Yunit> response = await Service(graph).SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.HttpFailure, response.State);
    Assert.AreEqual("(HttpRequestException) No such host is known", response.Message);
  }

  [TestMethod]
  public async Task SendEmails_SendsEachOne() {
    FakeGraph graph = new();

    List<ApiResponse<Yunit>> responses = await Service(graph).SendEmailsAsync([Email(), Email()]);

    Assert.AreEqual(2, responses.Count(r => r.State == ApiResponseStates.Success));
    Assert.AreEqual(2, graph.Requests.Count);
  }

  [TestMethod]
  public async Task BothServices_CanBeUsedThroughTheSenderInterface() {
    await using FakeSmtpServer server = new("PLAIN");
    FakeGraph graph = new();
    List<PixataEmailSenderInterface> senders = [new PixataEmailService(server.Settings()), Service(graph)];

    foreach (PixataEmailSenderInterface sender in senders) {
      Assert.AreEqual(ApiResponseStates.Success, (await sender.SendEmailAsync("billy@shears.com", "Hello", "<p>Hi</p>")).State);
    }

    Assert.AreEqual(1, server.Messages.Count);
    Assert.AreEqual(1, graph.Requests.Count);
  }
}
