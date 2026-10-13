using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Grid;
using Pixata.Blazor.Sample.Models;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Sample.Data;

// Reads employees for the PixataGrid page's OnRead example. Each read gets its own scope (and so its own DbContext), as two
// grids on the page can read at the same time, and a DbContext can't run two queries at once
public class GridSampleEmployeeService(IServiceScopeFactory scopeFactory) {
  public async Task<GridResult<GridSampleEmployee>> Read(GridRequest request, TimeSpan delay, CancellationToken cancellationToken) {
    // Slowed down on purpose, so you can see the loading indicator
    await Task.Delay(delay, cancellationToken);
    using IServiceScope scope = scopeFactory.CreateScope();
    SampleDbContext db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    // Ordered by Id when the user hasn't sorted, so the pages come out the same each time
    return await db.Employees.OrderBy(e => e.Id).ToGridResultAsync(request, cancellationToken);
  }
}
