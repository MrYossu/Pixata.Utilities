## Code guidelines

- File-scoped namespaces should always be used.
- Primary constructors should be used whenever possible.
- Opening braces should be on the end of the line, not on a new line.
- Code should be indented with 2 spaces, not tabs.
- Always use explicit variable types instead of `var` where possible.
- Use target-typed `new()` expressions where possible.
- Use expression-bodied members for single-line methods and properties.
- Always use braces in `if` statements and the like, even for single-line code blocks.
- Use collection initialisers (eg `[]`) instead of `new List<>()`.
- Use `""` instead of `string.Empty`.
- Use string interpolation instead of `String.Format()` or `+`.
- ASP.NET Core routes should never be hard-coded, but should be defined in a `RoutesHelper.cs` class.
- Interfaces should be named `AbcServiceInterface` and not `IAbcService`.
- Do not add an interface unless it is needed, eg for the services that are implemented multiple times. Helpers and services that are only ever implemented once do not need an interface.
- Async methods should not have 'Async' at the end of their names.
- Always use UK spelling for variable names, method names, etc. For example, "colour" not "color", "sanitise" not "sanitize".