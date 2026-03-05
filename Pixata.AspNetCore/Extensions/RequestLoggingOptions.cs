namespace Pixata.AspNetCore.Extensions;

public class RequestLoggingOptions {
  public string[] IgnoredPaths { get; set; } = ["_framework", "_blazor", "_content", ".well-known"];
  public bool LogBody { get; set; } = true;
}