using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Pixata.Blazor.Extensions;

public class CommonComponentBase<T> : ComponentBase where T : class {
  [Inject]
  public NavigationManager NavigationManager { get; set; } = null!;

  [Inject]
  public TemplateHelper TemplateHelper { get; set; } = null!;

  #region Authentication and claims

  [Inject]
  public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

  private ClaimsPrincipal _user;

  /// <summary>
  /// Determines whether the current user is authenticated.
  /// </summary>
  /// <returns>True if the user is authenticated, false if not</returns>
  public async Task<bool> IsAuthed() {
    _user ??= (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
    return _user.Identity!.IsAuthenticated;
  }

  /// <summary>
  /// Check if the user has a specific claim
  /// </summary>
  /// <param name="claim">The name of the claim to check</param>
  /// <returns>True if the user has the claim, false if the user is not authenticated or does not have the claim</returns>
  public async Task<bool> HasClaim(string claim) {
    _user ??= (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
    return _user.HasClaim(c => c.Type == claim);
  }

  /// <summary>
  /// Gets the value of a claim of the current user. If the user is not authenticated or does not have the claim, returns an empty string
  /// </summary>
  /// <param name="claim">The name of the claim to retrieve</param>
  /// <returns>The value of the claim, or an empty string if the claim is not found</returns>
  public async Task<string> GetClaim(string claim) {
    _user ??= (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
    return _user.FindFirst(claim)?.Value ?? string.Empty;
  }

  /// <summary>
  /// Gets the email address of the current user
  /// </summary>
  /// <returns>The email address of the current user, or an empty string if the user is not authenticated</returns>
  public async Task<string> GetEmail() {
    _user ??= (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
    return _user.Identity!.Name ?? "";
  }

  #endregion

  /// <summary>
  /// If set, this is the default style(s) to be applied to links and text
  /// </summary>
  public string DefaultStyle { get; set; } = "";

  /// <summary>
  /// If set, this is the default class(es) to be applied to links and text
  /// </summary>
  public string DefaultCssClass { get; set; } = "";

  /// <summary>
  /// If set, this is a function that returns the default CSS style(s) to be applied to links and text
  /// </summary>
  public Func<T, string>? DefaultStyleFunc { get; set; }

  /// <summary>
  /// If set, this is a function that returns the default CSS class(es) to be applied to links and text
  /// </summary>
  public Func<T, string>? DefaultCssClassFunc { get; set; }

  /// <summary>
  /// Converts an object of type <typeparamref name="T"/> to its string representation.
  /// </summary>
  public Func<T, string> ToText = _ => "";

  /// <summary>
  /// Render a link to a URI, using the ToText function to get the text for the link. This is what is called in the Template attribute of the component that is to be templated
  /// </summary>
  /// <param name="textFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This specifies what is displayed in the link</param>
  /// <param name="style">CSS styles to be applied to the link</param>
  /// <param name="cssClass">CSS classes to be applied to the link</param>
  /// <param name="styleFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This is used to specify CSS styles based on the object parameters</param>
  /// <param name="cssClassFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This is used to specify CSS classes based on the object parameters</param>
  /// <returns>A RenderFragment that Blazor will render</returns>
  public RenderFragment<object> Uri(Func<T, string> textFunc, string? style = null, string? cssClass = null, Func<T, string>? styleFunc = null, Func<T, string>? cssClassFunc = null) {
    string effectiveStyle = style ?? DefaultStyle;
    string effectiveCssClass = cssClass ?? DefaultCssClass;
    Func<T, string>? effectiveStyleFunc = styleFunc ?? DefaultStyleFunc;
    Func<T, string>? effectiveCssClassFunc = cssClassFunc ?? DefaultCssClassFunc;

    return effectiveStyleFunc != null || effectiveCssClassFunc != null
      ? TemplateHelper.Link(textFunc, ToText, effectiveStyleFunc ?? (_ => ""), effectiveCssClassFunc ?? (_ => ""))
      : TemplateHelper.Link(textFunc, ToText, effectiveStyle, effectiveCssClass);
  }

  /// <summary>
  /// Render text, using the ToText function. This is what is called in the Template attribute of the component that is to be templated
  /// </summary>
  /// <param name="textFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This specifies the text that is displayed</param>
  /// <param name="style">CSS styles to be applied to the text</param>
  /// <param name="cssClass">CSS classes to be applied to the text</param>
  /// <param name="styleFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This is used to specify CSS styles based on the object parameters</param>
  /// <param name="cssClassFunc">A Func&gt;T, string>&lt; that converts an object of type T to a string. This is used to specify CSS classes based on the object parameters</param>
  /// <returns>A RenderFragment that Blazor will render
  public RenderFragment<object> Text(Func<T, string> textFunc, string? style = null, string? cssClass = null, Func<T, string>? styleFunc = null, Func<T, string>? cssClassFunc = null) {
    string effectiveStyle = style ?? DefaultStyle;
    string effectiveCssClass = cssClass ?? DefaultCssClass;
    Func<T, string>? effectiveStyleFunc = styleFunc ?? DefaultStyleFunc;
    Func<T, string>? effectiveCssClassFunc = cssClassFunc ?? DefaultCssClassFunc;

    return effectiveStyleFunc != null || effectiveCssClassFunc != null
      ? TemplateHelper.Text(textFunc, effectiveStyleFunc ?? (_ => ""), effectiveCssClassFunc ?? (_ => ""))
      : TemplateHelper.Text(textFunc, effectiveStyle, effectiveCssClass);
  }
}