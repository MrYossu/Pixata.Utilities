using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Pixata.Blazor.Tests.Components;

/// <summary>
/// Creates a bUnit context for testing the components: JavaScript interop is loose (calls succeed and return defaults, so the
/// components' browser helpers don't need setting up), and the services from AddPixataBlazor() are registered
/// </summary>
public static class PixataTestContext {
  public static BunitContext Create() {
    BunitContext context = new();
    context.JSInterop.Mode = JSRuntimeMode.Loose;
    context.Services.AddPixataBlazor();
    return context;
  }
}
