using LanguageExt;

namespace Pixata.Blazor.LanguageExtComponents.Components {
  [Union]
  public interface LoadingOption<T> {
    LoadingOption<T> Loading();
    LoadingOption<T> Loaded(T value);
    LoadingOption<T> NotLoaded();
  }
}