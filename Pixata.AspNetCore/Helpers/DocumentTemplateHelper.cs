using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Http;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Pixata.AspNetCore.Helpers;

public class DocumentTemplateHelper(IHttpContextAccessor httpContextAccessor, HtmlRenderer htmlRenderer, IConverter htmlToPdfConverter) {
  public async Task<string> CreateHtmlFromTemplate<T>(params (string name, object? value)[] parameters) where T : IComponent =>
    await htmlRenderer.Dispatcher.InvokeAsync(async () => {
      HttpRequest request = httpContextAccessor.HttpContext!.Request;
      string baseUrl = $"{request.Scheme}://{request.Host}";
      Dictionary<string, object?> dictionary = new() {
        { "BaseUrl", baseUrl }
      };
      foreach ((string name, object? value) in parameters) {
        dictionary[name] = value;
      }
      HtmlRootComponent output = await htmlRenderer.RenderComponentAsync<T>(ParameterView.FromDictionary(dictionary));
      await using StringWriter writer = new();
      output.WriteHtmlTo(writer);
      return writer.ToString();
    });

  public async Task<byte[]> CreatePdfFromTemplate<T>(params (string name, object? value)[] parameters) where T : IComponent {
    string html = FixVoidElementsForPdf(await CreateHtmlFromTemplate<T>(parameters));
    HtmlToPdfDocument doc = new() {
      GlobalSettings = {
        ColorMode = ColorMode.Color,
        Orientation = Orientation.Portrait,
        PaperSize = PaperKind.A4Plus
      },
      Objects = {
        new ObjectSettings {
          PagesCount = true,
          HtmlContent = html,
          WebSettings = { DefaultEncoding = "utf-8" },
        }
      }
    };
    return htmlToPdfConverter.Convert(doc);
  }

  private static string FixVoidElementsForPdf(string html) =>
    Regex.Replace(html, @"<(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)(\s[^>]*)?>",
      m => m.Value.EndsWith("/>") ? m.Value : m.Value[..^1].TrimEnd() + " />",
      RegexOptions.IgnoreCase);
}