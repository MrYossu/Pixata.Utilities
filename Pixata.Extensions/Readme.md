# Pixata.Extensions [![Pixata.Extensions Nuget package](https://img.shields.io/nuget/v/Pixata.Extensions)](https://www.nuget.org/packages/Pixata.Extensions/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Extensions/VroumVroum.png "Pixata")

Some useful utility classes and methods I've developed over the past few years.

This is the shared kernel of the Pixata packages. It has no dependency on EF Core, ASP.NET Core or anything else that would stop you referencing it from a Blazor WASM app, so the types that both halves of a client/server feature need are kept here.

A [Nuget package](https://www.nuget.org/packages/Pixata.Extensions/) is available for this project.

## Documentation

The documentation is split over the following pages...

| Page | What's in it |
| --- | --- |
| [API responses](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.ApiResponse.md) | `ApiResponse<T>`, the result type used across these packages instead of exceptions, its states, and how to compose calls that return one. Also `Yunit`, which stands in for `void` |
| [String extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Strings.md) | Joining collections into strings, splitting camel case, working with multi-line strings, cleaning up strings and UK postcodes |
| [Number extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Numbers.md) | Ordinal suffixes, percentages, durations, file sizes, pluralisation, Hebrew numerals and fractions |
| [Date extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Dates.md) | Pretty date formatting, start and end of week, month and day, and human-friendly date ranges |
| [Collection extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Collections.md) | Observable collections, `ForEach`, `Flatten`, and synchronising a collection of entities with a collection of DTOs |
| [Object extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Objects.md) | Base64 encoding, shallow cloning and dumping an object's properties for debugging |
| [Exception extension methods](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Exceptions.md) | Flattening the `InnerException` stack into readable text |
| [Enum helpers](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Enums.md) | Listing the members of an `enum` with display-friendly names |
| [Caching](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.Caching.md) | `GetOrCreateSafe()`, which adds cache stampede protection to `IMemoryCache`, and the trouble with caching EF Core entities |
| [Shared models](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.SharedModels.md) | The auditing models, the encryption DTOs and `UploadFileDto`, which are shared between the client-side and server-side packages |

## Related packages

- [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor) - Blazor components, including one that renders an `ApiResponse<T>`
- [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore) - server-side helpers, auditing and payload encryption
- [Pixata.Email](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Email) - a MailKit wrapper that returns an `ApiResponse<Yunit>`
