using System.Collections.Generic;

namespace Pixata.Extensions.Grid;

/// <summary>
/// The result of applying a <see cref="GridRequest"/> to some data
/// </summary>
/// <param name="Data">The items to show, which is just the current page if paging was requested</param>
/// <param name="Total">The number of items before paging, which the grid's pager needs</param>
public record GridResult<T>(List<T> Data, int Total);
