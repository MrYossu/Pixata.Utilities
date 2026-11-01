using System.Collections.Generic;
using System.Threading.Tasks;
using Pixata.Extensions;

namespace Pixata.Email;

/// <summary>
/// Sends emails, however they are sent. Implemented by <see cref="PixataEmailService"/> (SMTP) and <see cref="PixataGraphEmailService"/>
/// (Microsoft Graph), so an app can choose between them, eg for each of its customers
/// </summary>
public interface PixataEmailSenderInterface {
  Task<ApiResponse<Yunit>> SendEmailAsync(string email, string subject, string htmlMessage);
  Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters);
  Task<List<ApiResponse<Yunit>>> SendEmailsAsync(IEnumerable<EmailParameters> emails);
}
