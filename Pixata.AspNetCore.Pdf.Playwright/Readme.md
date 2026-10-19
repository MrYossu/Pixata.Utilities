# Pixata.AspNetCore.Pdf.Playwright [![Pixata.AspNetCore.Pdf.Playwright Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.Playwright)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Playwright/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.Playwright/ordinateuroff.png "Pixata")

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using [Microsoft Playwright for .NET](https://playwright.dev/dotnet/) and Chromium. It turns the HTML rendered from your Blazor template into a PDF exactly as Chrome would print it, so templates can use any CSS that Chrome supports, including flexbox, grid and print CSS.

Playwright (Apache 2.0) and Chromium (BSD) are free, with no licence key. Chromium is copied into your app's build and publish output, so nothing needs installing on the server, and the app can be deployed to shared hosting.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Playwright/) is available for this project.

## Contents

- [Setup](#setup)
- [How Chromium gets to the server](#how-chromium-gets-to-the-server)
- [Deploying to shared hosting](#deploying-to-shared-hosting)
- [Writing templates for this converter](#writing-templates-for-this-converter)
- [Images and style sheets](#images-and-style-sheets)
- [Letterheads](#letterheads)
- [Performance](#performance)
- [Options](#options)

## Setup

This package is a converter for the `DocumentTemplateHelper` in Pixata.AspNetCore, so you register both. In `Program.cs`...

```csharp
builder.Services.AddPixataAspNetCore<Program>();
builder.Services.AddPixataPlaywrightPdf();
```

The first line registers `DocumentTemplateHelper` (along with the `HtmlRenderer` and `IHttpContextAccessor` it needs), which renders a Blazor component to HTML, and then hands the HTML to the converter registered by the second line to turn it into a PDF.

The type parameter can be any type in your app. It's only used to find your FluentValidation validators for Pixata.AspNetCore's validation endpoint filter, which is registered at the same time. `Program` is always there, so it's the easiest choice. If you don't use the validation filter, the non-generic `builder.Services.AddPixataAspNetCore()` registers everything else. See [registering services](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.md#registering-services) for how to opt out of the parts you don't want.

### The base URL

Templates are given a `BaseUrl` parameter for linking to images and style sheets on your site, and the converter only loads them from that site (see [images and style sheets](#images-and-style-sheets)). You don't need to set it, as the helper takes it from the scheme and host of the current request, so it is right in development, test and live without any changes. This works when you create PDFs from an endpoint, a controller, a Razor page, or a statically rendered or prerendering Blazor component.

There is no current request inside an interactive Blazor Server component (after the first render), or in a hosted or background service. If you create PDFs from either of those, pick up the base URL from the first request the app gets, by adding this to `Program.cs` after `UseHttpsRedirection()` (and after `UseForwardedHeaders()`, if the app is behind a reverse proxy)...

```csharp
PixataAspNetCoreOptions pixataOptions = app.Services.GetRequiredService<PixataAspNetCoreOptions>();
app.Use(async (HttpContext context, Func<Task> next) => {
  if (pixataOptions.DocumentBaseUrl == "") {
    pixataOptions.DocumentBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
  }
  await next();
});
```

An interactive Blazor Server component always follows a request (the one that loaded its page), so this always works there. A background service that creates a PDF before the site has had any requests gets an `InvalidOperationException` saying that the base URL can't be worked out.

As this trusts the `Host` header of the first request, set `AllowedHosts` in the live site's `appsettings.json` (eg `"AllowedHosts": "www.example.com"`), so that a request with a made-up host is rejected rather than becoming the base URL. Your host may already filter requests by host name for you.

Then use `DocumentTemplateHelper.CreatePdfFromTemplate` as described in [generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md). If you're switching from another converter, this registration is the only code change you need, although templates written for a different engine may print differently.

`AddPixataPlaywrightPdf()` takes an optional `Action<PlaywrightPdfOptions>` for [letterheads](#letterheads) and the other [options](#options). Invalid options throw an `ArgumentException` when you call it, rather than when the first PDF is created.

The browser isn't started until the first PDF is created, so registering the converter costs nothing at start-up.

## How Chromium gets to the server

Microsoft.Playwright copies its driver (a `.playwright` folder, about 100MB) into your app's output. This package adds Chromium's headless shell (a `.playwright-browsers` folder, about 230MB), so that everything the converter needs is in the published files.

- **When the app is built**, the package works out which build of Chromium the referenced version of Playwright needs. If the build machine doesn't have it yet, it downloads it, once, into the machine's Playwright browser cache (`%LOCALAPPDATA%\ms-playwright` on Windows). You'll see a message in the build output when this happens. The build machine needs internet access the first time; the server never does.
- **The browser is then copied** into `bin` with the rest of the app, and into the publish folder when you publish, whether that's `dotnet publish`, a Visual Studio publish profile, or a CI pipeline. Upload the publish folder as usual. It never goes under `wwwroot`, so it can't be downloaded from your site.
- **When you upgrade** Microsoft.Playwright (directly, or by upgrading this package), the next build downloads and copies the matching Chromium automatically. Old ones are left in the build machine's cache. Deploy the new `.playwright` and `.playwright-browsers` folders with the rest of the app. Most upload tools skip files that haven't changed, so these only take a while when the version changes.
- **Only en-US locale files are copied**, which saves about 40MB. Chromium only uses them for its own messages, not for the text in your PDFs.
- **Build on the same operating system as the server.** The browser downloaded is the one for the build machine's operating system, so publish for a Windows server from Windows.

You can control this with MSBuild properties in your app's project file...

| Property | Default | What it does |
| --- | --- | --- |
| `PixataPlaywrightBundleBrowser` | `true` | Set to `false` to not copy Chromium, eg if you use the `Channel` or `BrowserExecutablePath` [options](#options) |
| `PixataPlaywrightAllLocales` | `false` | Set to `true` to copy all of Chromium's locale files |
| `PixataPlaywrightBrowserCache` | Playwright's cache | Where the build machine keeps downloaded browsers. Uses `PLAYWRIGHT_BROWSERS_PATH` if that's set on the build machine |

If you reference this project with a `ProjectReference` rather than as a NuGet package, MSBuild doesn't import the package's targets, so also add `<Import Project="path\to\Pixata.AspNetCore.Pdf.Playwright\build\Pixata.AspNetCore.Pdf.Playwright.targets" />` to the app's project file.

The longest path Chromium adds is about 120 characters below the app's folder. If Windows long paths aren't enabled on the build machine, a build or publish in a very deeply nested folder can fail with "could not copy" errors. Build somewhere with a shorter path if so.

## Deploying to shared hosting

The converter is designed to work where you have no access to the server: nothing is installed, no environment variables or registry settings are needed, and no user profile is used. Chromium is started from the app's own folder, with the app pool's identity.

What the converter can't fix is a host that doesn't allow it at all. Some shared hosts...

- don't let a site start other processes, or run `.exe` files from its folder;
- have antivirus that removes or blocks files in `.playwright-browsers`;
- limit a site's memory to less than Chromium needs (see [performance](#performance) for how much it uses);
- don't let the site write to the server's temporary folder. If so, set the `TempFolder` [option](#options) to a folder inside the app, eg `App_Data/PdfTemp`.

So, before you rely on it, deploy the app and create a test PDF. If the browser can't be found or started, the first PDF throws an `InvalidOperationException` saying what it tried, where it looked, and what to check. Show your host that message if you need them to change something.

Taking the app offline (eg with `app_offline.htm`, which Visual Studio and Web Deploy do for you) shuts the app down, and the converter closes Chromium when that happens, so its files aren't locked when the new version is uploaded. Chromium is also closed if the app is stopped or recycled. If the app process is killed outright, the Playwright driver notices and closes Chromium itself.

## Writing templates for this converter

Chromium prints your HTML the way Chrome does, so you can use flexbox, grid, web fonts and print CSS such as `break-before`, `break-inside: avoid` and `@page`. You can check how a template will look by opening its HTML in Chrome and using print preview.

A few things to know...

- **JavaScript is turned off.** Templates are rendered on the server, so they shouldn't need it, and it means that anything user-supplied in the HTML can't run scripts.
- **`@page` margins in a template override `PdfOptions.MarginMm`.** A `@page` size is ignored unless you set the `PreferCssPageSize` [option](#options), so that `PdfOptions` controls the paper size.
- **Backgrounds are printed**, so background colours and images in your CSS appear in the PDF.
- **Fonts** come from the server, so if a template uses a font the server may not have (shared hosts rarely let you install fonts), load it as a web font from your site.

## Images and style sheets

Images, style sheets and fonts are loaded over HTTP, but only from the site in `PdfOptions.BaseUrl` (which `DocumentTemplateHelper` sets for you from `DocumentBaseUrl` or the current request), or from `data:` URIs. Relative URLs are resolved against `BaseUrl`, as they would be on your site. Anything else (another site, an internal address, `file://`) is blocked. A blocked image is replaced with a blank one, so it leaves a gap rather than a broken image icon. This means that user-supplied content in a template, such as a client's notes on an invoice, can't make your server fetch arbitrary URLs.

A redirect is only followed if its target is on the same site. Each PDF is created in its own isolated browser context, so nothing (cookies, cache) carries over from one PDF to the next.

## Letterheads

There are two ways to put a letterhead on every page. You can use either, or both.

### Converter options

Set a header and/or footer when you register the converter, and it is added to every PDF, with no changes to your templates or the code that creates the PDFs...

```csharp
builder.Services.AddPixataPlaywrightPdf(o => {
  o.Header = new() { HeightMm = 30, ImagePath = "wwwroot/images/letterhead.png" };
  o.Footer = new() { HeightMm = 15, ImagePath = "wwwroot/images/letterhead-footer.png", PageNumbers = "Page {page} of {pages}" };
});
```

- The header goes at the very top of each page, and the footer at the very bottom, from edge to edge. The top and bottom margins are increased by their heights, so `PdfOptions.MarginMm` is the gap between the letterhead and the document's content.
- The image is stretched to the full width of the page, and scaled to fit `HeightMm`, keeping its aspect ratio. For an image that fills the strip, make its aspect ratio the page width to `HeightMm`, eg 2100x300 pixels for a 30mm header on A4 (210mm wide).
- `ImagePath` is relative to the app's content root, and can be a PNG, JPEG, GIF, WebP or SVG. If the image is in memory, set `Image` (a `byte[]`) and `ImageContentType` instead. The image is read once, when the first PDF is created.
- `PageNumbers` is optional, with `{page}` and `{pages}` for the page number and the number of pages. It is shown on top of the image, so leave space in the image where it goes. `PageNumberAlignment`, `PageNumberFontSizePt` and `PageNumberMarginMm` control where it goes and how big it is.
- For anything else, set `Html` to your own HTML for the header or footer. Chromium renders this on its own, so it can't load images or style sheets: use inline styles, and `data:` URIs for images. Elements with the classes `pageNumber` and `totalPages` are filled in with the page number and number of pages. Chromium gives the header the whole top margin (and the footer the whole bottom one), and adds its own padding, which you can remove with `<style>#header,#footer{padding:0 !important}</style>`.

To leave the letterhead off a particular document, eg one with a letterhead of its own, add this to its template's `<head>`...

```html
<meta name="pixata-pdf-letterhead" content="none" />
```

The same header and footer are used on every page, so this can't give the first page a different letterhead from the rest. A letterhead at the top of a template's body only appears on the first page, which may be enough.

### Print CSS in the template

A template can have its own letterhead, using CSS. This is handy if different documents need different letterheads, as it lives with the template, and its images load from your site like any others. If the converter options also set a letterhead, both appear, so add the `pixata-pdf-letterhead` meta tag above to templates that have their own.

Elements with `position: fixed` are repeated on every page. To keep the content clear of them, put it in a table whose header and footer rows are the height of the letterhead, as Chromium repeats those on every page too...

```html
<head>
  <meta name="pixata-pdf-letterhead" content="none" />
  <style>
    @page { margin: 0; }
    body { margin: 0; }
    .letterhead { position: fixed; top: 0; left: 0; width: 100%; height: 30mm; }
    .letterhead-footer { position: fixed; bottom: 0; left: 0; width: 100%; height: 15mm; }
    .page { width: 100%; border-collapse: collapse; }
    .page > thead td { height: 40mm; padding: 0; } /* the letterhead, plus a 10mm gap */
    .page > tfoot td { height: 25mm; padding: 0; }
    .page > tbody td { padding: 0 10mm; }
  </style>
</head>
<body>
  <img class="letterhead" src="@BaseUrl/images/letterhead.png" />
  <img class="letterhead-footer" src="@BaseUrl/images/letterhead-footer.png" />
  <table class="page">
    <thead><tr><td></td></tr></thead>
    <tfoot><tr><td></td></tr></tfoot>
    <tbody><tr><td>
      <!-- The document's content -->
    </td></tr></tbody>
  </table>
</body>
```

For text, page numbers or a small logo in the margins, Chromium also supports `@page` margin boxes...

```css
@page {
  margin: 40mm 10mm 25mm 10mm;
  @top-left { content: url('https://www.example.com/images/logo.svg'); }
  @bottom-right { content: "Page " counter(page) " of " counter(pages); font-size: 9pt; }
}
```

An image in a margin box is shown at its natural size, so this works best with an SVG, or an image saved at the size you want it.

## Performance

One Chromium is started the first time a PDF is created, and shared by every PDF after that. Each PDF gets its own lightweight browser context, which is closed when the PDF is done. If Chromium crashes, the next PDF starts a new one.

As a rough guide, on a desktop PC, the first PDF took about 1.2 seconds (most of it starting Chromium), and each one after that about 250ms. Creating 20 three-page invoices one after the other took about 6.5 seconds. Creating them at the same time took about 1.7 seconds, with the default of 4 at once. Your timings will depend on the server and the templates.

Once started, Chromium and the Playwright driver stay running, using about 160MB of memory between PDFs, and about 400MB while creating 4 at once, on top of your app.

`MaxConcurrentConversions` limits how many PDFs are created at once (each uses a browser tab), and others wait their turn. Raise it if the server has memory to spare, or lower it to 1 on a host with tight memory limits.

## Options

| Option | Default | What it does |
| --- | --- | --- |
| `Header` | `null` | A letterhead (and/or page numbers) at the top of every page, see [letterheads](#letterheads) |
| `Footer` | `null` | A letterhead (and/or page numbers) at the bottom of every page |
| `MaxConcurrentConversions` | `4` | The most PDFs created at the same time |
| `TimeoutSeconds` | `30` | How long starting Chromium, loading the document (including its images) or creating the PDF can take before it fails |
| `PreferCssPageSize` | `false` | Use a page size set with `@page { size: ... }` in the template, rather than `PdfOptions.PaperSize` and `Orientation` |
| `TempFolder` | `""` | A folder for Chromium's temporary files, for hosts where the server's temporary folder isn't writable. Relative to the content root, eg `App_Data/PdfTemp`. While the Playwright driver starts (once, when the first PDF is created), the process's `TMP`, `TEMP` and `TMPDIR` environment variables are briefly set to this folder, as there's no other way to pass it to the driver |
| `BrowserExecutablePath` | `""` | The path to a Chromium executable to use instead of the copied one. Relative to the content root |
| `Channel` | `""` | Use a browser installed on the server instead of the copied one, eg `msedge` or `chrome`. Only for servers you control. Set `PixataPlaywrightBundleBrowser` to `false` in the project file if you use this, so that the build doesn't copy Chromium |
