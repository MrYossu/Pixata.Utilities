# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A solution of independently versioned NuGet packages (`Pixata.*`), plus a sample Blazor Server app that exercises them. Every package is published to nuget.org from CI. There is no single "app" here — changes are library changes, and each one lands as a version bump on the affected package.

## Commands

Build/test a single project (fast, and what you normally want):

```bash
dotnet build Pixata.Extensions/Pixata.Extensions.csproj -v q --nologo
```

```bash
dotnet test Pixata.Extensions.Tests/Pixata.Extensions.Tests.csproj -v q --nologo
```

Run one test:

```bash
dotnet test Pixata.Extensions.Tests/Pixata.Extensions.Tests.csproj --filter "FullyQualifiedName~StringExtensionMethods_Tests"
```

**Solution-wide `dotnet restore`/`build`/`test` needs Telerik feed credentials.** `nuget.config` adds `https://nuget.telerik.com` with `%TELERIK_USERNAME%`/`%TELERIK_PASSWORD%`. Without them the feed answers 401 (not 404) for *any* package it is asked about, which is a hard `NU1301` failure rather than a skipped source — so a solution-level restore fails even for projects that have nothing to do with Telerik. Prefer per-project commands unless the Telerik env vars are set.

Test projects are MSTest (`Pixata.Extensions.Tests`, `Pixata.Blazor.Tests`); they target `net8.0` only, even where the package under test multi-targets.

## Package layout and dependency direction

```
Pixata.Extensions          — no project deps; the shared kernel
  ├── Pixata.Blazor        — Razor components (client-safe)
  ├── Pixata.AspNetCore    — server-side (EF Core, wkhtmltopdf, FluentValidation)
  ├── Pixata.Email         — MailKit wrapper
  └── Pixata.SimilarityChooser

Pixata.Blazor.TelerikComponents  — Telerik components, client-safe
Pixata.AspNetCore.Telerik        — server half of the above (EF Core + SqlClient)
Pixata.Google, Pixata.Functional, Pixata.Blazor.LanguageExtComponents — legacy/unmaintained (LanguageExt)
Pixata.Blazor.Sample             — consumes the packages via ProjectReference; deployed to test.pixata.co.uk by CI
```

The recurring architectural rule is **the client/server split**: anything that needs EF Core, SQL Server, or `HttpContext` must not land in a package a WASM project references. That is why `Pixata.AspNetCore.Telerik` exists (server half of the Telerik grid helpers), why the audit viewer's server service lives in `Pixata.AspNetCore` while its components live in `Pixata.Blazor`, and why `Pixata.Extensions` deliberately has no EF Core dependency. When adding something, decide which side it belongs on before choosing a project.

Multi-targeting: the actively maintained packages target `net8.0;net10.0` with per-TFM `PackageReference` blocks (8.0.x vs 10.0.0). `Pixata.Blazor.TelerikComponents`, `Pixata.Google` and `Pixata.SimilarityChooser` are still `net8.0` only. Adding a dependency to a multi-targeted project means adding it to *both* conditional `ItemGroup`s.

## Cross-cutting features (each spans several packages)

- **`ApiResponse<T>`** (`Pixata.Extensions/ApiResponse.cs`) — the result type used everywhere instead of exceptions. It's a monad: `Select`/`SelectMany`/`Match`, implicit conversion to `bool` for success, plus async `SelectMany` extensions so LINQ query syntax composes async API calls. `Yunit` is the void-substitute returned by the `Action`-based `Match`. `Pixata.Blazor/Containers/ApiResponseView.razor` renders one.
- **Auditing** — models and interfaces in `Pixata.Extensions/Auditing`, the EF Core `AuditingInterceptor` + services + minimal-API endpoints in `Pixata.AspNetCore/Auditing`, viewer components in `Pixata.Blazor/Auditing`. Wired up as `AddAuditing<TDbContext>()` + `AddAuditingInterceptor()` + `AddPixataAuditViewer()`; see `Pixata.Blazor.Sample/Program.cs`.
- **Payload encryption** — DTOs/options in `Pixata.Extensions/Encryption`, browser SubtleCrypto encryptor + `DelegatingHandler` in `Pixata.Blazor/Encryption` (JS in `Pixata.Blazor/wwwroot/crypto-interop.js`), AES-GCM decryption middleware + handshake endpoint + session key store in `Pixata.AspNetCore/Encryption`.
- **Styling** — the components ship their own CSS at `Pixata.Blazor/wwwroot/pixata.css`, consumed by hosts as `_content/Pixata.Blazor/pixata.css`; it is CSS-variable driven so hosts can retheme. Do not reintroduce a Bootstrap dependency.

## DI registration convention

Each package exposes one `AddPixataXxx()` extension. `AddPixataBlazor()` checks for already-registered services and logs rather than double-registering. `AddPixataAspNetCore<T>()` takes a `PixataAspNetCoreOptions` action so callers can opt out of parts they don't want (the wkhtmltopdf converter is registered as a *factory* so the native library isn't loaded at startup), and throws a clear registration-time exception when an opted-out dependency is still needed. Follow that pattern — fail loudly at registration, not at resolve time — when adding registrations.

## Release process

1. Change code in the package.
2. Bump `<Version>`, `<AssemblyVersion>` and `<FileVersion>` (all three) in that package's `.csproj`.
3. Document the change in that package's `Readme.md` — these are shipped as `PackageReadmeFile`, so they are the actual package documentation, not just repo notes.
4. Commit. Version-bump commits are conventionally named `Pixata.Blazor v4.2.0` (or `Pixata.AspNetCore v1.8.0, Pixata.Blazor v2.32.0, …` for several at once); behaviour commits use a plain imperative sentence.
5. Pushing to `master` builds and tests. Pushing to `release` publishes every package to nuget.org and FTP-deploys the sample site — only ever push to `release` when asked.

A new package must be added to *both* `.github/workflows/master.yml` and `release.yml` (restore + build steps, and a publish step in `release.yml`); the workflows list projects explicitly, and an unrestored project makes the solution-wide `dotnet test` step fail with `NU1301`.

## Code style

From `.github/copilot-instructions.md` — applies to `.cs` files and `@code` blocks in `.razor` alike:

- 2-space indent, opening brace on the same line, file-scoped namespaces, primary constructors where possible.
- **Explicit types, not `var`**, wherever a type can be written; target-typed `new()` where it can.
- Braces on every `if`/loop body, even single statements. Expression-bodied members for one-liners.
- Collection expressions (`[]`) over `new List<>()`; `""` over `string.Empty`; interpolation over `string.Format`/`+`.
- Initialise parameters/properties; `= null!` for non-nullable references the framework or DI will set.
- Interfaces are named `AbcServiceInterface`, **not** `IAbcService`. Don't add an interface unless something is actually implemented more than once.
- Async methods do **not** end in `Async`.
- UK spelling throughout identifiers and text ("colour", "sanitise", "initialise"). Sentence case for titles ("Terms and conditions").
- ASP.NET Core routes go in a `RoutesHelper.cs`, never hard-coded.
