using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pixata.Email.Tests;

/// <summary>
/// Just enough of an SMTP server (no TLS) to see what the email service says to it. It can offer logins (AUTH) or not, and records the
/// AUTH commands it is sent and the messages it receives
/// </summary>
public sealed class FakeSmtpServer : IAsyncDisposable {
  private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
  private readonly CancellationTokenSource _stop = new();
  private readonly Task _accepting;
  private readonly string _authMechanisms;

  /// <param name="authMechanisms">The mechanisms listed in the reply to EHLO, eg <c>PLAIN LOGIN</c>, or empty to not offer logins</param>
  public FakeSmtpServer(string authMechanisms) {
    _authMechanisms = authMechanisms;
    _listener.Start();
    _accepting = Accept();
  }

  public int Port =>
    ((IPEndPoint)_listener.LocalEndpoint).Port;

  /// <summary>
  /// Each AUTH command, with any responses to the server's challenges joined on with spaces, eg <c>AUTH PLAIN AGppbQBzZWNyZXQ=</c>
  /// </summary>
  public List<string> AuthCommands { get; } = [];

  /// <summary>
  /// The DATA of each message received
  /// </summary>
  public List<string> Messages { get; } = [];

  public int Connections { get; private set; }

  public SmtpSettings Settings(string userName = "", string password = "") =>
    new() {
      Server = "127.0.0.1",
      Port = Port,
      SocketOptions = MailKit.Security.SecureSocketOptions.None,
      UserName = userName,
      Password = password,
      FromEmail = "jim@spriggs.com",
      FromName = "Jim Spriggs"
    };

  private async Task Accept() {
    try {
      while (!_stop.IsCancellationRequested) {
        using TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token);
        Connections++;
        await Converse(client);
      }
    }
    catch (OperationCanceledException) {
    }
    catch (ObjectDisposedException) {
    }
  }

  private async Task Converse(TcpClient client) {
    await using NetworkStream stream = client.GetStream();
    using StreamReader reader = new(stream, Encoding.ASCII);
    await using StreamWriter writer = new(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
    await writer.WriteLineAsync("220 localhost Fake ESMTP");
    while (await reader.ReadLineAsync(_stop.Token) is { } line) {
      string command = line.Split(' ')[0].ToUpperInvariant();
      switch (command) {
        case "EHLO":
          await writer.WriteLineAsync("250-localhost");
          if (_authMechanisms != "") {
            await writer.WriteLineAsync($"250-AUTH {_authMechanisms}");
          }
          await writer.WriteLineAsync("250 8BITMIME");
          break;
        case "HELO":
          await writer.WriteLineAsync("250 localhost");
          break;
        case "AUTH":
          await Authenticate(line, reader, writer);
          break;
        case "MAIL":
        case "RCPT":
        case "RSET":
        case "NOOP":
          await writer.WriteLineAsync("250 OK");
          break;
        case "DATA":
          await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
          StringBuilder data = new();
          while (await reader.ReadLineAsync(_stop.Token) is { } dataLine && dataLine != ".") {
            data.AppendLine(dataLine);
          }
          Messages.Add(data.ToString());
          await writer.WriteLineAsync("250 OK queued");
          break;
        case "QUIT":
          await writer.WriteLineAsync("221 Bye");
          return;
        default:
          await writer.WriteLineAsync("502 Command not implemented");
          break;
      }
    }
  }

  private async Task Authenticate(string line, StreamReader reader, StreamWriter writer) {
    string[] parts = line.Split(' ');
    if (_authMechanisms == "") {
      await writer.WriteLineAsync("503 Authentication not enabled");
      return;
    }
    string recorded = line;
    string mechanism = parts[1].ToUpperInvariant();
    if (mechanism == "LOGIN") {
      foreach (string prompt in parts.Length > 2 ? ["UGFzc3dvcmQ6"] : new[] { "VXNlcm5hbWU6", "UGFzc3dvcmQ6" }) {
        await writer.WriteLineAsync($"334 {prompt}");
        recorded += $" {await reader.ReadLineAsync()}";
      }
    } else if (parts.Length == 2) {
      // No initial response, so ask for it
      await writer.WriteLineAsync("334 ");
      recorded += $" {await reader.ReadLineAsync()}";
    }
    AuthCommands.Add(recorded);
    await writer.WriteLineAsync("235 2.7.0 Authentication successful");
  }

  public async ValueTask DisposeAsync() {
    await _stop.CancelAsync();
    _listener.Stop();
    try {
      await _accepting;
    }
    catch (SocketException) {
    }
    _stop.Dispose();
  }
}
