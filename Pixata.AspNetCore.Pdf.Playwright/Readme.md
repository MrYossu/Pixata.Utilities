# Pixata.AspNetCore.Pdf.Playwright [![Pixata.AspNetCore.Pdf.Playwright Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.Playwright)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Playwright/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.Playwright/ordinateuroff.png "Pixata")

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using [Microsoft Playwright for .NET](https://playwright.dev/dotnet/) and Chromium. It turns the HTML rendered from your Blazor template into a PDF exactly as Chrome would print it, so templates can use any CSS that Chrome supports, including flexbox, grid and print CSS. This is the recommended converter.

Playwright (Apache 2.0) and Chromium (BSD) are free, with no licence key. Chromium is copied into your app's build and publish output, so nothing needs installing on the server, and the app can be deployed to shared hosting.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Playwright/) is available for this project.

>**Version 1.1.1** works in Linux containers. The Playwright driver's `node` executable was copied into the build and publish output so that only the user who built the app could run it, so an app in an image based on `mcr.microsoft.com/dotnet/aspnet` (which runs as the `app` user) couldn't start it. The build now makes it executable by everyone, and if it still can't be run, the error says why. The readme has a new section on [Linux and Docker](#linux-and-docker), with a sample Dockerfile, the libraries Chromium needs, and how much it adds to the image (#145)

>**Version 1.1.0** shortens the paths Chromium needs below your app's folder by about 40 characters, so that it fits in an ordinary project folder on Windows without long paths. **Deployment change:** Chromium is now copied to `.pw\<revision>` (eg `.pw\1243`) in the build and publish output, in place of `.playwright-browsers\chromium_headless_shell-<revision>`, so `PlaywrightPdfConverter.BrowsersFolder` is now `".pw"`. Deploy the new `.pw` folder with the rest of the app as usual, and delete the old `.playwright-browsers` folder from the server once the new version is running. If you've told antivirus or your host about `.playwright-browsers`, tell them about `.pw` instead. On Windows without long paths, a build or publish to a folder that's still too deep now stops with one error saying what to do, rather than an error for every file that can't be copied. See [long paths on Windows](#long-paths-on-windows) (#126)

>**Version 1.0.2** changes the package description and this readme to say that this is the recommended converter. No code changes.

>**Version 1.0.1** fixes blocked images showing as light blue boxes in the PDF. The blank image used in place of a blocked one was meant to be transparent, but was half-transparent blue, so each blocked image (one that isn't on the site in `PdfOptions.BaseUrl`, see [images and style sheets](#images-and-style-sheets)) appeared as a blue rectangle the size of the image. It now leaves a gap, as intended.

## Contents

- [Setup](#setup)
- [How Chromium gets to the server](#how-chromium-gets-to-the-server)
- [Deploying to shared hosting](#deploying-to-shared-hosting)
- [Linux and Docker](#linux-and-docker)
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

Microsoft.Playwright copies its driver (a `.playwright` folder, about 100MB) into your app's output. This package adds Chromium's headless shell (a `.pw` folder, about 230MB), so that everything the converter needs is in the published files.

- **When the app is built**, the package works out which build of Chromium the referenced version of Playwright needs. If the build machine doesn't have it yet, it downloads it, once, into the machine's Playwright browser cache (`%LOCALAPPDATA%\ms-playwright` on Windows, `~/.cache/ms-playwright` on Linux). You'll see a message in the build output when this happens. The build machine needs internet access the first time; the server never does.
- **The browser is then copied** into `bin` with the rest of the app, and into the publish folder when you publish, whether that's `dotnet publish`, a Visual Studio publish profile, or a CI pipeline. Upload the publish folder as usual. It never goes under `wwwroot`, so it can't be downloaded from your site.
- **When you upgrade** Microsoft.Playwright (directly, or by upgrading this package), the next build downloads and copies the matching Chromium automatically. Old ones are left in the build machine's cache. Deploy the new `.playwright` and `.pw` folders with the rest of the app. Most upload tools skip files that haven't changed, so these only take a while when the version changes.
- **Only en-US locale files are copied**, which saves about 40MB. Chromium only uses them for its own messages, not for the text in your PDFs.
- **Build on the same operating system as the server.** The browser downloaded is the one for the build machine's operating system, so publish for a Windows server from Windows, and for a Linux server or container from Linux (see [Linux and Docker](#linux-and-docker)).

You can control this with MSBuild properties in your app's project file...

| Property | Default | What it does |
| --- | --- | --- |
| `PixataPlaywrightBundleBrowser` | `true` | Set to `false` to not copy Chromium, eg if you use the `Channel` or `BrowserExecutablePath` [options](#options) |
| `PixataPlaywrightAllLocales` | `false` | Set to `true` to copy all of Chromium's locale files |
| `PixataPlaywrightBrowserCache` | Playwright's cache | Where the build machine keeps downloaded browsers. Uses `PLAYWRIGHT_BROWSERS_PATH` if that's set on the build machine |
| `PixataPlaywrightCheckPathLength` | `true` | Set to `false` to not stop the build when Chromium's paths would be too long for Windows (see [long paths on Windows](#long-paths-on-windows)) |

If you reference this project with a `ProjectReference` rather than as a NuGet package, MSBuild doesn't import the package's targets, so also add `<Import Project="path\to\Pixata.AspNetCore.Pdf.Playwright\build\Pixata.AspNetCore.Pdf.Playwright.targets" />` to the app's project file.

### Long paths on Windows

Unless long paths are enabled, Windows can't use a path of 260 characters or more, and Chromium's deepest file is about 105 characters below the build or publish folder...

```
.pw\1243\chrome-headless-shell-win64\PrivacySandboxAttestationsPreloaded\privacy-sandbox-attestations.dat
```

So the build output folder (eg `C:\Users\Jim\source\repos\MyApp\MyApp\bin\Release\net10.0\`) and the publish folder need to be shorter than about 155 characters. A Visual Studio publish to a folder copies the files twice, first to `obj\Release\net10.0\PubTmp\Out\` in the project and then to the folder in the publish profile, so both need to fit. A [git worktree](https://git-scm.com/docs/git-worktree) inside the project folder adds its path to all of these.

When long paths aren't enabled on a Windows build machine, the build checks this before it copies anything. If a path would be too long, it stops with one error that gives the path and its length, rather than an MSB3021 "could not copy" error for every file. To fix it, do one of these...

- **Enable long paths in Windows.** Set `LongPathsEnabled` to `1` in `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem` (or turn on the "Enable Win32 long paths" group policy, under Computer Configuration > Administrative Templates > System > Filesystem), then restart Visual Studio. You need to be an administrator to do this. This is the setting MSBuild itself checks.
- **Build or publish to a folder with a shorter path**, eg by moving the solution nearer the root of the drive, or by setting the publish profile's folder to something like `C:\Publish\MyApp`.
- **Don't copy Chromium.** Set `PixataPlaywrightBundleBrowser` to `false`, and use the `Channel` [option](#options) to use a browser installed on the server, or `BrowserExecutablePath` to point at one you deploy yourself.

The files are uploaded to the server with the same paths below the app's folder, so keep that short too. Most hosts' site folders (eg `C:\inetpub\wwwroot\MySite`) are.

If you know the check is wrong for your machine, set `PixataPlaywrightCheckPathLength` to `false` in the project file to turn it off.

## Deploying to shared hosting

The converter is designed to work where you have no access to the server: nothing is installed, no environment variables or registry settings are needed, and no user profile is used. Chromium is started from the app's own folder, with the app pool's identity.

What the converter can't fix is a host that doesn't allow it at all. Some shared hosts...

- don't let a site start other processes, or run `.exe` files from its folder;
- have antivirus that removes or blocks files in `.pw`;
- limit a site's memory to less than Chromium needs (see [performance](#performance) for how much it uses);
- don't let the site write to the server's temporary folder. If so, set the `TempFolder` [option](#options) to a folder inside the app, eg `App_Data/PdfTemp`.

So, before you rely on it, deploy the app and create a test PDF. If the browser can't be found or started, the first PDF throws an `InvalidOperationException` saying what it tried, where it looked, and what to check. Show your host that message if you need them to change something.

Taking the app offline (eg with `app_offline.htm`, which Visual Studio and Web Deploy do for you) shuts the app down, and the converter closes Chromium when that happens, so its files aren't locked when the new version is uploaded. Chromium is also closed if the app is stopped or recycled. If the app process is killed outright, the Playwright driver notices and closes Chromium itself.

## Linux and Docker

The converter works on Linux, including in a container, but Chromium needs some shared libraries that a minimal Linux install (and the `mcr.microsoft.com/dotnet/aspnet` images) doesn't have. Without them, the first PDF throws an `InvalidOperationException` whose message includes Chromium's own error, such as `error while loading shared libraries: libglib-2.0.so.0`.

### A sample Dockerfile

This publishes the app in a Linux build stage, so the Chromium copied into the output is the Linux one, and then installs the libraries Chromium needs in the runtime image...

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MyApp/MyApp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# The Playwright driver knows which libraries its Chromium needs, and how they're named in the image's version of Linux, so let it install
# them. It's copied first, so that this layer is only rebuilt when the version of Playwright changes, not every time the app does
COPY --from=build /app/.playwright .playwright
RUN .playwright/node/linux-x64/node .playwright/package/cli.js install-deps chromium-headless-shell \
 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

A few things to know...

- **The build stage needs internet access**, to download Chromium the first time (from `cdn.playwright.dev`), as well as for the NuGet packages. The image never does, as Chromium is in the published files. On an ARM64 machine, use `linux-arm64` in place of `linux-x64`.
- **`install-deps` uses `apt-get`**, so it works in the Debian and Ubuntu images (the .NET 8 images are Debian 12 by default, and the .NET 10 ones Ubuntu 24.04), but not in the Alpine ones, which Chromium doesn't support. It must run before `USER`, as root.
- **If you'd rather list the packages yourself**, this is what Chromium needs on Ubuntu 24.04 (the .NET 10 images, or `8.0-noble`)...

  ```dockerfile
  RUN apt-get update \
   && apt-get install -y --no-install-recommends libasound2t64 libatk-bridge2.0-0t64 libatk1.0-0t64 libatspi2.0-0t64 libcairo2 libcups2t64 \
        libdbus-1-3 libdrm2 libgbm1 libglib2.0-0t64 libnspr4 libnss3 libpango-1.0-0 libx11-6 libxcb1 libxcomposite1 libxdamage1 libxext6 \
        libxfixes3 libxkbcommon0 libxrandr2 fonts-liberation fonts-noto-color-emoji \
   && rm -rf /var/lib/apt/lists/*
  ```

  On Debian 12 (the .NET 8 images), drop the `t64` from the ends of the names. This is about 90MB smaller than what `install-deps` adds, as that also installs fonts for Chinese, Japanese, Thai and the rest, and `xvfb`, which the headless shell doesn't need.
- **Fonts.** The images have no fonts at all. `install-deps` adds Liberation (which has the same widths as Arial, Times New Roman and Courier New, so text fits as it would on Windows), FreeFont (which has Hebrew and Greek, among others), and fonts for Chinese, Japanese, Thai and emoji. The shorter list above only has Liberation and emoji, so add the fonts you need (such as `fonts-noto-core`), or load them as web fonts from your site. Microsoft's own fonts, such as Arial and Calibri, aren't there, so a template that asks for them gets Liberation or another font in their place.
- **The app's user.** The .NET images run the app as the `app` user, not as root. The build makes the Playwright driver executable by everyone (versions before 1.1.1 didn't), and Chromium already is. If you see "Permission denied" when the first PDF is created, run `chmod a+x .playwright/node/*/node` in the build stage, after publishing.
- **A read-only root filesystem.** Chromium and the Playwright driver need somewhere to write their temporary files, which is `/tmp` unless you say otherwise. With `docker run --read-only`, add `--tmpfs /tmp` (or an `emptyDir` volume at `/tmp` in Kubernetes). Or mount a writable folder elsewhere, and set the `TempFolder` [option](#options) to it. `/dev/shm` isn't needed, as Playwright tells Chromium not to use it, so Docker's small default size for it doesn't matter.
- **The base URL.** In a container, the address the app is reached on from outside (through a reverse proxy, or a port mapping) may not be one the container can reach itself on. The converter fetches the template's images and style sheets from the [base URL](#the-base-url), so if they come out missing, set `PixataAspNetCoreOptions.DocumentBaseUrl` to an address that works from inside the container, such as `http://localhost:8080` (the port the .NET images listen on).

As a rough guide, with Microsoft.Playwright 1.63 on x64, Chromium (`.pw`) adds about 215MB to the published app, and the Playwright driver (`.playwright`) about 135MB, of which 120MB is its own copy of Node.js. The libraries and fonts that `install-deps` installs add about 330MB more, or 240MB with the shorter list, so the image is about 700MB bigger than the app alone would be. In a test container, the first PDF took about 0.75 seconds (most of it starting Chromium), and each one after that about 75ms. With Chromium running, the whole container used about 170MB of memory, of which the app was about 65MB. See [performance](#performance) for more.

CI runs this package's tests, which create real PDFs, on Linux as well as Windows.

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
