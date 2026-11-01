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
