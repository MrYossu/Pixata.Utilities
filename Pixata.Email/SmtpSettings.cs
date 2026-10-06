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

    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
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
