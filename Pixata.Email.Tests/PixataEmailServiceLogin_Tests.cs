using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions;

namespace Pixata.Email.Tests;

[TestClass]
public class PixataEmailServiceLogin_Tests {
  private static EmailParameters Email(int n = 1) =>
    new($"Email {n}", $"<p>Hello {n}</p>", "billy@shears.com", "Billy Shears");

  private static string Decode(string base64) =>
    Encoding.UTF8.GetString(Convert.FromBase64String(base64));

  [TestMethod]
  public async Task NoUserName_DoesNotLogIn() {
    await using FakeSmtpServer server = new("PLAIN LOGIN");
    PixataEmailService service = new(server.Settings());

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(0, server.AuthCommands.Count);
    Assert.AreEqual(1, server.Messages.Count);
    StringAssert.Contains(server.Messages[0], "Subject: Email 1");
  }

  [TestMethod]
  public async Task ServerWithoutLogins_DoesNotLogIn() {
    await using FakeSmtpServer server = new("");
    PixataEmailService service = new(server.Settings("jim", "secret"));

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(1, server.Messages.Count);
  }

  [TestMethod]
  public async Task UserName_LogsInWithThePassword() {
    await using FakeSmtpServer server = new("PLAIN");
    PixataEmailService service = new(server.Settings("jim", "secret"));

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    string auth = server.AuthCommands.Single();
    StringAssert.StartsWith(auth, "AUTH PLAIN ");
    Assert.AreEqual("\0jim\0secret", Decode(auth.Split(' ').Last()));
    Assert.AreEqual(1, server.Messages.Count);
  }

  [TestMethod]
  public async Task AccessToken_LogsInWithOAuth() {
    await using FakeSmtpServer server = new("PLAIN LOGIN XOAUTH2");
    SmtpSettings settings = server.Settings("jim@spriggs.com", "not used");
    settings.AccessToken = "the-token";
    PixataEmailService service = new(settings);

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    string auth = server.AuthCommands.Single();
    StringAssert.StartsWith(auth, "AUTH XOAUTH2 ");
    Assert.AreEqual("user=jim@spriggs.com\u0001auth=Bearer the-token\u0001\u0001", Decode(auth.Split(' ').Last()));
    Assert.AreEqual(1, server.Messages.Count);
  }

  [TestMethod]
  public async Task AccessTokenWithoutAUserName_SaysTheUserNameIsNeeded() {
    await using FakeSmtpServer server = new("XOAUTH2");
    SmtpSettings settings = server.Settings();
    settings.AccessToken = "the-token";
    PixataEmailService service = new(settings);

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email());

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    StringAssert.Contains(response.Message, "UserName must be the mailbox's email address");
    Assert.AreEqual(0, server.Messages.Count);
  }

  [TestMethod]
  public async Task WithSocketOptions_NoUserName_DoesNotLogIn() {
    await using FakeSmtpServer server = new("PLAIN LOGIN");
    PixataEmailService service = new(server.Settings());

    ApiResponse<Yunit> response = await service.SendEmailAsync(Email(), MailKit.Security.SecureSocketOptions.None);

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    Assert.AreEqual(0, server.AuthCommands.Count);
    Assert.AreEqual(1, server.Messages.Count);
  }

  [TestMethod]
  public async Task SendEmails_NoUserName_SendsThemAllOverOneConnectionWithoutLoggingIn() {
    await using FakeSmtpServer server = new("PLAIN LOGIN");
    PixataEmailService service = new(server.Settings());

    List<ApiResponse<Yunit>> responses = await service.SendEmailsAsync([Email(1), Email(2), Email(3)]);

    Assert.IsTrue(responses.All(r => r.State == ApiResponseStates.Success), string.Join(", ", responses.Select(r => r.Message)));
    Assert.AreEqual(0, server.AuthCommands.Count);
    Assert.AreEqual(3, server.Messages.Count);
    Assert.AreEqual(1, server.Connections);
  }

  [TestMethod]
  public async Task SendEmails_UserName_LogsInOnce() {
    await using FakeSmtpServer server = new("PLAIN");
    PixataEmailService service = new(server.Settings("jim", "secret"));

    List<ApiResponse<Yunit>> responses = await service.SendEmailsAsync([Email(1), Email(2)]);

    Assert.IsTrue(responses.All(r => r.State == ApiResponseStates.Success), string.Join(", ", responses.Select(r => r.Message)));
    Assert.AreEqual(1, server.AuthCommands.Count);
    Assert.AreEqual(2, server.Messages.Count);
  }
}
