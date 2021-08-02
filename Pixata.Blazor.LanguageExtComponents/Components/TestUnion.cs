using LanguageExt;

namespace Pixata.Blazor.LanguageExtComponents.Components {
  [Union]
  public interface TestUnion<T> {
    TestUnion<T> Jim();
    TestUnion<T> Fred(T value);
  }
}