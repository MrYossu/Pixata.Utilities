using System;

namespace Pixata.Functional {
  public static class FallibleExt {
    /// <summary>
    /// Matches the three states of the Fallible, and performs the appropriate action. If f is BadIdea, then onFailure is used
    /// </summary>
    /// <typeparam name="T">The generic type of the incoming Fallible</typeparam>
    /// <param name="f">The Fallible being passed in</param>
    /// <param name="onSuccess">An Action to be performed when f is Success&lt;T&gt;</param>
    /// <param name="onFailure">An Action to be performed when f is Failure&lt;T&gt; or BadIdea&lt;T&gt;</param>
    public static void Match<T>(this Fallible<T> f, Action<T> onSuccess, Action<Exception> onFailure) =>
      f.Match(onSuccess, onFailure, msg => onFailure(new Exception(msg)));

    /// <summary>
    /// Matches the three states of the Fallible and performs the appropriate action
    /// </summary>
    /// <typeparam name="T">The generic type of the incoming Fallible</typeparam>
    /// <param name="f">The Fallible being passed in</param>
    /// <param name="onSuccess">An Action to be performed when f is Success&lt;T&gt;</param>
    /// <param name="onFailure">An Action to be performed when f is Failure&lt;T&gt;</param>
    /// <param name="onBadIdea">An Action to be performed when f is BadIdea&lt;T&gt;</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if f is none of the known Fallible types. This could probably only happen if Fallible were extended (unlikely), but Match was not updated to match (pardon the pun!)</exception>
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

    /// <summary>
    /// Matches the three states of the Fallible&lt;T&gt; and returns a &lt;TOut&gt; by calling the appropriate Func
    /// </summary>
    /// <typeparam name="T">The generic type of the incoming Fallible</typeparam>
    /// <typeparam name="TOut">The generic type of the return value from onSuccess</typeparam>
    /// <param name="f">The Fallible being passed in</param>
    /// <param name="onSuccess">A Func to be performed when f is Success&lt;T&gt;. Converts T to TOut</param>
    /// <param name="onFailure">A Func to be performed when f is Failure&lt;T&gt;. Converts Exception to TOut</param>
    /// <param name="onBadIdea">A Func to be performed when f is BadIdea&lt;T&gt;. Converts string to TOut</param>
    /// <returns>A TOut</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if f is none of the known Fallible types. This could probably only happen if Fallible were extended (unlikely), but Match was not updated to match (pardon the pun!)</exception>
    public static TOut Match<T, TOut>(this Fallible<T> f, Func<T, TOut> onSuccess, Func<Exception, TOut> onFailure, Func<string, TOut> onBadIdea) {
      switch (f) {
        case Success<T> success:
          return onSuccess(success.Value);
        case BadIdea<T> badIdea:
          return onBadIdea(badIdea.Reason);
        case Failure<T> failure:
          return onFailure(failure.Ex);
        default:
          throw new ArgumentOutOfRangeException($"Unknown Fallible type {f.GetType().Name}");
      }
    }

    /// <summary>
    /// Run an action and have a Failure generated automatically in case of an exception
    /// </summary>
    /// <typeparam name="T">The generic type of the incoming Fallible</typeparam>
    /// <param name="a">The Action to run. This should produce either a Success&lt;T&gt; or a BadIdea&lt;T&gt;. Exceptions will automatically be returned in a Failure</param>
    /// <returns></returns>
    public static Fallible<T> Run<T>(Func<Fallible<T>> a) {
      try {
        return a();
      } catch (Exception ex) {
        return Fallible.Failure<T>(ex);
      }
    }
  }
}