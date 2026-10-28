using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Auditing.Components;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class AuditViewer_Tests {
  private class FakeAuditViewerService : AuditViewerServiceInterface {
    public int EntityIdLoads { get; private set; }
    public int AllEntryLoads { get; private set; }
    public bool FailEntityTypes { get; set; }
    public bool FailLoads { get; set; }

    public Task<List<EntityTypeMetadata>> GetEntityTypes() =>
      FailEntityTypes
        ? throw new InvalidOperationException("The database is down")
        : Task.FromResult<List<EntityTypeMetadata>>([new() { FullName = "Sample.Customer", ShortName = "Customer" }]);

    public Task<List<string>> GetDistinctEntityIds(string entityType) {
      EntityIdLoads++;
      return FailLoads
        ? throw new InvalidOperationException("A second operation was started on this context instance")
        : Task.FromResult<List<string>>(["1", "2", "10"]);
    }

    public Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId) =>
      Task.FromResult<List<AuditEntryViewModel>>([]);

    public Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId, DateTime? fromDate, DateTime? toDate, string? user, AuditOperation? operation) =>
      Task.FromResult<List<AuditEntryViewModel>>([]);

    public Task<List<AuditEntryViewModel>> GetAllAuditEntries(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) {
      AllEntryLoads++;
      return FailLoads
        ? throw new InvalidOperationException("A second operation was started on this context instance")
        : Task.FromResult<List<AuditEntryViewModel>>([]);
    }
  }

  private static (BunitContext context, FakeAuditViewerService service, NavigationManager navigation) Create(string uri = "audit") {
    BunitContext context = PixataTestContext.Create();
    FakeAuditViewerService service = new();
    context.Services.AddSingleton<AuditViewerServiceInterface>(service);
    NavigationManager navigation = context.Services.GetRequiredService<NavigationManager>();
    navigation.NavigateTo(uri);
    return (context, service, navigation);
  }

  [TestMethod]
  public void AuditViewer_HasNoInlineStyleSheet_SoPixataCssCanRethemeIt() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddScoped<AuditViewerServiceInterface, FakeAuditViewerService>();
    IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
    cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll(".pixata-blazor-audit-viewer").Count));
    Assert.AreEqual(0, cut.FindAll("style").Count);
  }

  [TestMethod]
  public void AuditViewer_ReloadsWhenTheQueryStringChanges() {
    (BunitContext context, FakeAuditViewerService service, NavigationManager navigation) = Create();
    using (context) {
      IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
      cut.WaitForAssertion(() => Assert.AreEqual(1, service.AllEntryLoads));

      navigation.NavigateTo("audit?entityType=Sample.Customer");

      cut.WaitForAssertion(() => Assert.AreEqual(1, service.EntityIdLoads));
      Assert.AreEqual(3, cut.FindAll(".pixata-blazor-audit-view-btn").Count);
    }
  }

  [TestMethod]
  public void AuditViewer_DoesNotReloadWhenTheUserLeavesThePage() {
    (BunitContext context, FakeAuditViewerService service, NavigationManager navigation) = Create("audit?entityType=Sample.Customer");
    using (context) {
      IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
      cut.WaitForAssertion(() => Assert.AreEqual(1, service.EntityIdLoads));

      navigation.NavigateTo("users");
      navigation.NavigateTo("users?entityType=Sample.Customer");

      Assert.AreEqual(1, service.EntityIdLoads);
      Assert.AreEqual(0, service.AllEntryLoads);
    }
  }

  [TestMethod]
  public void AuditViewer_IgnoresTheCaseOfThePathAndATrailingSlash() {
    (BunitContext context, FakeAuditViewerService service, NavigationManager navigation) = Create("Audit");
    using (context) {
      IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
      cut.WaitForAssertion(() => Assert.AreEqual(1, service.AllEntryLoads));

      navigation.NavigateTo("audit/?entityType=Sample.Customer");

      cut.WaitForAssertion(() => Assert.AreEqual(1, service.EntityIdLoads));
    }
  }

  [TestMethod]
  public void AuditViewer_ShowsAnErrorWhenReloadingFails_RatherThanLettingItEscape() {
    (BunitContext context, FakeAuditViewerService service, NavigationManager navigation) = Create();
    using (context) {
      IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
      cut.WaitForAssertion(() => Assert.AreEqual(1, service.AllEntryLoads));
      Assert.AreEqual(0, cut.FindAll(".pixata-blazor-audit-error-state").Count);

      service.FailLoads = true;
      navigation.NavigateTo("audit?entityType=Sample.Customer");

      cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(".pixata-blazor-audit-error-state").TextContent, "A second operation was started"));
      Assert.AreEqual(0, cut.FindAll(".pixata-blazor-audit-loading-state").Count);

      // A load that works clears the error
      service.FailLoads = false;
      navigation.NavigateTo("audit?allView=true");

      cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll(".pixata-blazor-audit-error-state").Count));
    }
  }

  [TestMethod]
  public void AuditViewer_ShowsAnErrorWhenTheFirstLoadFails() {
    (BunitContext context, FakeAuditViewerService service, NavigationManager _) = Create();
    using (context) {
      service.FailEntityTypes = true;
      IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();

      cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(".pixata-blazor-audit-error-state").TextContent, "Couldn't load the entity types. The database is down"));
      Assert.AreEqual(0, service.AllEntryLoads);
    }
  }
}
