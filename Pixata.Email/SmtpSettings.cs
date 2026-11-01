using System;
using MailKit.Security;

namespace Pixata.Email {
  public class SmtpSettings {
    public string Server { get; set; } = "";
    public int Port { get; set; }

    /// <summary>
    /// How the connection to the SMTP server is secured. When this is null, <see cref="UseSsl"/> is used if it was set, otherwise
    /// the connection uses <see cref="SecureSocketOptions.SslOnConnect"/> on port 465, and <see cref="SecureSocketOptions.StartTls"/>
    /// (required, not opportunistic) on any other port
    /// </summary>
    public SecureSocketOptions? SocketOptions { get; set; }

    /// <summary>
    /// Legacy setting. True maps to <see cref="SecureSocketOptions.SslOnConnect"/>, false to <see cref="SecureSocketOptions.StartTlsWhenAvailable"/>.
    /// Ignored when <see cref="SocketOptions"/> is set
    /// </summary>
    [Obsolete("Use SocketOptions instead. UseSsl = true fails on port 587, and UseSsl = false only uses STARTTLS if the server offers it")]
    public bool? UseSsl { get; set; }

    /// <summary>
    /// The user name to log in to the SMTP server with. Leave this empty for a server that doesn't need a login, such as an internal relay
    /// that allows the app's IP address, or a local test server. The service also doesn't log in if the server doesn't offer it
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The password to log in to the SMTP server with. Ignored when <see cref="UserName"/> is empty, or when <see cref="AccessToken"/> is set
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// An OAuth access token to log in with (XOAUTH2), in place of the password, eg for Microsoft 365 or Gmail. <see cref="UserName"/> must be
    /// the mailbox's email address. Getting the token, and getting a new one when it expires, is up to the app
    /// </summary>
    public string AccessToken { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public string ReplyTo { get; set; } = "";

    /// <summary>
    /// The socket options that will actually be used to connect, worked out from <see cref="SocketOptions"/>, <see cref="UseSsl"/> and <see cref="Port"/>
    /// </summary>
    public SecureSocketOptions EffectiveSocketOptions() {
      if (SocketOptions is { } socketOptions) {
        return socketOptions;
      }
#pragma warning disable CS0618
      if (UseSsl is { } useSsl) {
        return useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
      }
#pragma warning restore CS0618
      return Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
  }
}
