using System;

namespace Pixata.Extensions {
  public static class ExceptionExtensions {
    /// <summary>
    /// Returns the exception messages all the way down the InnerException stack
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <returns></returns>
    public static string MessageStack(this Exception ex) =>
      MessageStack(ex, Environment.NewLine);

    public static string MessageStack(this Exception ex, string separator) {
      string msg = $"{ex.Message}{separator}{ex.StackTrace}{separator}";
      Exception innerException = ex.InnerException;
      while (innerException != null) {
        msg += $"{separator}{innerException.Message}{separator}{innerException.StackTrace}{separator}";
        innerException = innerException.InnerException;
      }

      return msg;
    }

    /// <summary>
    /// Gets the innermost exception in a stack
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <returns>The innermost exception</returns>
    public static string InnerType(this Exception ex) {
      string type = ex.GetType().ToString();
      Exception innerException = ex.InnerException;
      while (innerException != null) {
        type = innerException.GetType().ToString();
        innerException = innerException.InnerException;
      }

      return type;
    }
  }
}