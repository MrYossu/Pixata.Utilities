using LanguageExt;

namespace Pixata.Blazor.LanguageExtComponents.Components {
  [Union]
  public interface LoaderContainer<A> {
    LoaderContainer<A> Loading();
    LoaderContainer<A> Loaded(A value);
    LoaderContainer<A> NotLoaded();
  }
}