namespace Pixata.Email {
  public class SmtpSettings {
    public string Server { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
  }
}