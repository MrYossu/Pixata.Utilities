using Pixata.Extensions;
using System.Threading.Tasks;

namespace Pixata.Email {
  public interface PixataEmailServiceInterface {
    Task<ApiResponse<Yunit>> SendEmailAsync(string email, string subject, string htmlMessage);
    Task<ApiResponse<Yunit>> SendEmailAsync(EmailParameters emailParameters);
  }
}