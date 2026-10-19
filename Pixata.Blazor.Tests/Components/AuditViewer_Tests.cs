using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Auditing.Components;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class AuditViewer_Tests {
  private class FakeAuditViewerService : AuditViewerServiceInterface {
    public Task<List<EntityTypeMetadata>> GetEntityTypes() =>
      Task.FromResult<List<EntityTypeMetadata>>([new() { FullName = "Sample.Customer", ShortName = "Customer" }]);

    public Task<List<string>> GetDistinctEntityIds(string entityType) =>
      Task.FromResult<List<string>>(["1", "2", "10"]);

    public Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId) =>
      Task.FromResult<List<AuditEntryViewModel>>([]);

    public Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId, DateTime? fromDate, DateTime? toDate, string? user, AuditOperation? operation) =>
      Task.FromResult<List<AuditEntryViewModel>>([]);

    public Task<List<AuditEntryViewModel>> GetAllAuditEntries(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) =>
      Task.FromResult<List<AuditEntryViewModel>>([]);
  }

  [TestMethod]
  public void AuditViewer_HasNoInlineStyleSheet_SoPixataCssCanRethemeIt() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddScoped<AuditViewerServiceInterface, FakeAuditViewerService>();
    IRenderedComponent<AuditViewer> cut = context.Render<AuditViewer>();
    cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll(".pixata-blazor-audit-viewer").Count));
    Assert.AreEqual(0, cut.FindAll("style").Count);
  }
}
