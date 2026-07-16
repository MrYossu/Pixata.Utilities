using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace Pixata.Blazor.Extensions;

public class ScrollStateService(ILocalStorageService localStorage) {
  public async Task Save(string key, double scrollTop) =>
    await localStorage.SetItemAsync(key, scrollTop);

  public async Task<double?> Get(string key) =>
    await localStorage.GetItemAsync<double?>(key);

  public async Task Remove(string key) =>
    await localStorage.RemoveItemAsync(key);
}