using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Notifications;
using Pixata.Extensions;

namespace Pixata.Blazor.Tests;

[TestClass]
public class ApiResponseView_Tests {
  private const string Key = "people";

  private BunitContext _ctx = null!;
  private PersistentComponentStateHarness _harness = null!;
  private int _loads;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _loads = 0;
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    ApiResponseViewConfig.PersistState = true;
    ApiResponseViewConfig.MaxPersistedBytes = null;
  }

  // A page that was prerendered with "From the prerender" as the data, or a prerender if there's no state
  private void Setup(bool prerendered) {
    _harness = new(prerendered ? new Dictionary<string, byte[]> { [Key] = Encoding.UTF8.GetBytes("""{"state":1,"data":"From the prerender"}""") } : null);
    _ctx.Services.AddSingleton(_harness.State);
    _ctx.Services.AddScoped<NotificationHelper>();
  }

  private IRenderedComponent<ApiResponseView<string>> Render(Action<ComponentParameterCollectionBuilder<ApiResponseView<string>>>? more = null) =>
    _ctx.Render<ApiResponseView<string>>(p => {
      p.Add(v => v.Key, Key)
        .Add(v => v.LoadApiResponse, () => {
          _loads++;
          return Task.FromResult(new ApiResponse<string>(ApiResponseStates.Success, "Loaded"));
        })
        .Add(v => v.Success, data => b => b.AddContent(0, data));
      more?.Invoke(p);
    });

  [TestMethod]
  public void PrerenderedData_IsUsed_WithoutLoadingItAgain() {
    Setup(prerendered: true);

    IRenderedComponent<ApiResponseView<string>> view = Render();

    Assert.AreEqual(0, _loads);
    StringAssert.Contains(view.Markup, "From the prerender");
  }

  [TestMethod]
  public void PersistStateFalse_LoadsTheDataAgain() {
    Setup(prerendered: true);

    IRenderedComponent<ApiResponseView<string>> view = Render(p => p.Add(v => v.PersistState, false));

    Assert.AreEqual(1, _loads);
    StringAssert.Contains(view.Markup, "Loaded");
  }

  [TestMethod]
  public void ConfigPersistStateFalse_LoadsTheDataAgain() {
    ApiResponseViewConfig.PersistState = false;
    Setup(prerendered: true);

    IRenderedComponent<ApiResponseView<string>> view = Render();

    Assert.AreEqual(1, _loads);
    StringAssert.Contains(view.Markup, "Loaded");
  }

  [TestMethod]
  public void ConfigPersistStateFalse_CanBeOverriddenForOneView() {
    ApiResponseViewConfig.PersistState = false;
    Setup(prerendered: true);

    IRenderedComponent<ApiResponseView<string>> view = Render(p => p.Add(v => v.PersistState, true));

    Assert.AreEqual(0, _loads);
    StringAssert.Contains(view.Markup, "From the prerender");
  }

  [TestMethod]
  public async Task Prerendering_PersistsTheData() {
    Setup(prerendered: false);
    Render();

    await _harness.RunPersistCallbacks();

    Assert.IsTrue(_harness.Persisted.ContainsKey(Key));
  }

  [TestMethod]
  public async Task Prerendering_WithPersistStateFalse_PersistsNothing() {
    Setup(prerendered: false);
    Render(p => p.Add(v => v.PersistState, false));

    await _harness.RunPersistCallbacks();

    Assert.IsFalse(_harness.Persisted.ContainsKey(Key));
  }

  [TestMethod]
  public async Task Prerendering_DataBiggerThanMaxPersistedBytes_IsNotPersisted_AndAWarningIsLogged() {
    ApiResponseViewConfig.MaxPersistedBytes = 10;
    ListLoggerFactory loggers = new();
    _ctx.Services.AddSingleton<ILoggerFactory>(loggers);
    Setup(prerendered: false);
    Render();

    await _harness.RunPersistCallbacks();

    Assert.IsFalse(_harness.Persisted.ContainsKey(Key));
    Assert.AreEqual(1, loggers.Warnings.Count);
    StringAssert.Contains(loggers.Warnings[0], "people");
    StringAssert.Contains(loggers.Warnings[0], "MaxPersistedBytes");
  }

  [TestMethod]
  public async Task Prerendering_DataWithinMaxPersistedBytes_IsPersisted() {
    ApiResponseViewConfig.MaxPersistedBytes = 1000;
    Setup(prerendered: false);
    Render();

    await _harness.RunPersistCallbacks();

    Assert.IsTrue(_harness.Persisted.ContainsKey(Key));
  }

  private sealed class ListLoggerFactory : ILoggerFactory {
    public List<string> Warnings { get; } = [];

    public ILogger CreateLogger(string categoryName) =>
      new ListLogger(this);

    public void AddProvider(ILoggerProvider provider) {
    }

    public void Dispose() {
    }

    private sealed class ListLogger(ListLoggerFactory factory) : ILogger {
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        null;

      public bool IsEnabled(LogLevel logLevel) =>
        true;

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (logLevel == LogLevel.Warning) {
          factory.Warnings.Add(formatter(state, exception));
        }
      }
    }
  }
}
