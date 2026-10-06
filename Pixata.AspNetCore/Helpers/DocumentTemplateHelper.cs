using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Http;
using Pixata.AspNetCore.Pdf;

namespace Pixata.AspNetCore.Helpers;

/// <summary>
/// Renders a Blazor component to HTML, or to a PDF if a <see cref="PdfConverterInterface"/> has been registered
/// </summary>
public class DocumentTemplateHelper(HtmlRenderer htmlRenderer, PixataAspNetCoreOptions? options = null, IHttpContextAccessor? httpContextAccessor = null, PdfConverterInterface? pdfConverter = null) {
  public async Task<string> CreateHtmlFromTemplate<T>(params (string name, object? value)[] parameters) where T : IComponent {
    // Worked out before switching to the renderer's dispatcher, as the HttpContext is tied to the calling thread's execution context
    string baseUrl = GetBaseUrl();
    return await htmlRenderer.Dispatcher.InvokeAsync(async () => {
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
  }

  public Task<byte[]> CreatePdfFromTemplate<T>(params (string name, object? value)[] parameters) where T : IComponent =>
    CreatePdfFromTemplate<T>(new PdfOptions(), parameters);

  public async Task<byte[]> CreatePdfFromTemplate<T>(PdfOptions pdfOptions, params (string name, object? value)[] parameters) where T : IComponent {
    if (pdfConverter is null) {
      throw new InvalidOperationException($"No {nameof(PdfConverterInterface)} has been registered. Add the Pixata.AspNetCore.Pdf.Telerik package and call AddPixataTelerikPdf(), or register an implementation of your own");
    }
    string html = await CreateHtmlFromTemplate<T>(parameters);
    if (string.IsNullOrWhiteSpace(pdfOptions.BaseUrl)) {
      pdfOptions.BaseUrl = GetBaseUrl();
    }
    return await pdfConverter.Convert(html, pdfOptions);
  }

  private string GetBaseUrl() {
    if (!string.IsNullOrWhiteSpace(options?.DocumentBaseUrl)) {
      return options.DocumentBaseUrl.TrimEnd('/');
    }
    if (httpContextAccessor?.HttpContext?.Request is { } request) {
      return $"{request.Scheme}://{request.Host}";
    }
    throw new InvalidOperationException($"There is no HttpContext to work out the base URL from, which happens in interactive Blazor Server components, background services and tests. Set {nameof(PixataAspNetCoreOptions)}.{nameof(PixataAspNetCoreOptions.DocumentBaseUrl)} when you call AddPixataAspNetCore");
  }
}
