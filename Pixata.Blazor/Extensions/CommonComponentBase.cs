using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Extensions;

public class CommonComponentBase<T> : ComponentBase where T : class {
  [Inject]
  public NavigationManager NavigationManager { get; set; } = null!;

  [Inject]
  public TemplateHelper TemplateHelper { get; set; } = null!;

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