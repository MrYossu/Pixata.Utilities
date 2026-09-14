# Caching

>Part of [Pixata.Extensions](Readme.md)

The `MemoryCacheExtensions` class adds cache stampede protection to `IMemoryCache`.

If you aren't caching your data, then I strongly recommend you watch [this short video](https://www.youtube.com/watch?v=Q49SZlxskjs) before reading on! It's by no means the only video out there, but it's short, clear and shows the benefits of caching in a very compelling way.

For people developing big sites, who are probably using a distributed cache, then the .NET `HybridCache` is ideal. However, for most of us, who develop single-instance apps, this is not necessary, and can be a suboptimal choice due to the extra implicit behaviour you must understand, less obvious performance characteristics and a framework-level dependency. For most of us, the `IMemoryCache` is a better choice.

The one major advantage that the hybrid cache has even for smaller apps is that it has built-in cache stampede protection, meaning that if multiple requests come in for the same uncached item, only one of them will trigger the expensive data retrieval operation, and the others will wait for the result to be cached. If you use a memory cache, you need to implement this yourself, which is not hard, but adds extra noise to your code.

The class here adds a `GetOrCreateSafe()` extension method to `IMemoryCache` to provide a simple way to add cache stampede protection, with no more code than you would have anyway. It is intended to replace the regular `GetOrCreate()` method. Basic usage is as follows...

```csharp
Product? product = await cache.GetOrCreateSafe($"product-{id}",
                                               async _ => await dbContext.Products.SingleOrDefault(p => p.Id == id),
                                               TimeSpan.FromMinutes(10));
```

**Important:** see the note below about caching entities.

In reality, you should store your cache keys in a centralised place, and not hard-code them like this, but this was kept simple to show the syntax.

The lambda includes a cancellation token (which is ignored in the above example), which can be passed through if needed...

```csharp
Product? product = await cache.GetOrCreateSafe($"product-{id}",
                                               async ct => await repository.GetByIdAsync(id, ct),
                                               TimeSpan.FromMinutes(5));
```

The time to live is optional, as is a `CancellationToken` for the call itself.

## Avoiding tracking exceptions

Suppose your products were associated with categories, and as the categories do not change very often, you decide to cache them. The cached categories will be associated with the database context from which they were pulled. If, on a subsequent request, you want to associate a product with a category, you would use one of the cached ones, and then scratch your head when EF threw an exception about a duplicate Id.

The way to avoid this is to check the Ids of the categories you need, then pass those to a method that attaches the categories you need to the current context...

```csharp
  private async Task<List<Category>> GetTrackedCategories(List<int> ids) {
    List<Category> cached = await GetCachedCategories();
    List<Category> tracked = [];
    foreach (Category category in cached.Where(l => ids.Contains(l.Id))) {
      Category? existing = _context.Categories.Local.FindEntry(category.Id)?.Entity;
      if (existing is not null) {
        tracked.Add(existing);
      } else {
        _context.Attach(category);
        tracked.Add(category);
      }
    }
    return tracked;
  }

  private async Task<List<Category>> GetCachedCategories() =>
    await memoryCache.GetOrCreateSafe(CacheKeysHelper.Categories, async ct =>
      await _context.Categories.ToListAsync(cancellationToken: ct));
```

This is all a bit nasty, but I haven't found a better way to do it yet.

Instead of caching database entities (which can be large, and are not always ideal candidates for caching, see note above), you can convert to DTOs and cache those...

```csharp
ProductDto? dto = await cache.GetOrCreateSafe($"product-{id}",
                                              async ct => {
                                                Product entity = await repository.GetByIdAsync(id, ct);
                                                return new ProductDto {
                                                  Id = entity.Id,
                                                  Name = entity.Name,
                                                  Price = entity.Price
                                                };
                                              },
                                              TimeSpan.FromMinutes(10));
```
