using System;

namespace Pixata.Functional {
  public static class FallibleExtensions {
    public static void Match<T>(this Fallible<T> f, Action<T> onSuccess, Action<Exception> onFailure) =>
      f.Match(onSuccess, onFailure, _ => Actions.Empty());

    public static void Match<T>(this Fallible<T> f, Action<T> onSuccess, Action<Exception> onFailure, Action<string> onBadIdea) {
      switch (f) {
        case Success<T> success:
          onSuccess(success.Value);
          break;
        case BadIdea<T> badIdea:
          onBadIdea(badIdea.Reason);
          break;
        case Failure<T> failure:
          onFailure(failure.Ex);
          break;
        default:
          throw new ArgumentOutOfRangeException($"Unknown Fallible type {f.GetType().Name}");
      }
    }
  }
}