using System;
using System.Diagnostics.Contracts;
using LanguageExt;

namespace Pixata.Functional {
  [Free]
  // ReSharper disable once InconsistentNaming
  public interface Fallible<T> {
    [Pure]
    T Success(T value);

    [Pure]
    string BadIdea(string reason);

    [Pure]
    Exception Failure(Exception ex);
  }
}