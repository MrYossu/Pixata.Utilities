using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Pixata.Blazor.Extensions;

public class PasswordOptionsHelper(IOptions<IdentityOptions> identityOptions) {
  public string PasswordOptions(bool your = true) {
    string options = $"<div class=\"password-requirements\">{(your ? "Your" : "The")} password must be at least {identityOptions.Value.Password.RequiredLength} characters long, and contain...<ul>";

    if (identityOptions.Value.Password.RequireDigit) {
      options += "<li>at least one digit</li>";
    }
    if (identityOptions.Value.Password.RequireLowercase) {
      options += "<li>at least one lowercase letter</li>";
    }
    if (identityOptions.Value.Password.RequireUppercase) {
      options += "<li>at least one uppercase letter</li>";
    }
    if (identityOptions.Value.Password.RequireNonAlphanumeric) {
      options += "<li>at least one special character # $ ^ + = ! * ( ) @ % &</li>";
    }
    if (identityOptions.Value.Password.RequiredUniqueChars > 1) {
      options += $"<li>at least {identityOptions.Value.Password.RequiredUniqueChars} unique characters</li>";
    }
    options += "</ul></div>";
    return options;
  }
}