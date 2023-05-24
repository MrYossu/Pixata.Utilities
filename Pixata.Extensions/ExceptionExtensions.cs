using System;

namespace Pixata.Extensions {
  public static class ExceptionExtensions {
    /// <summary>
    /// Returns the exception messages all the way down the InnerException stack, separated by Environment.NewLine
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <returns></returns>
    public static string MessageStack(this Exception ex) =>
      MessageStack(ex, Environment.NewLine);

    /// <summary>
    /// Returns the exception messages all the way down the InnerException stack, separated by the "separator" parameter
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <param name="separator">Separator between messages. Use &lt;br/gt; for display in HTML, or Environment.NewLine (default) for plain text.</param>
    /// <returns></returns>
    public static string MessageStack(this Exception ex, string separator) {
      string msg = $"{ex.Message}{separator}{ex.StackTrace}{separator}";
      Exception innerException = ex.InnerException;
      while (innerException != null) {
        msg += $"{separator}{innerException.Message}{separator}{innerException.StackTrace}";
        innerException = innerException.InnerException;
      }
      return msg;
    }

    /// <summary>
    /// Returns the exception messages all the way down the InnerException stack, but without any stack traces, separated by Environment.NewLine
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <returns></returns>
    public static string Messages(this Exception ex) =>
      Messages(ex, Environment.NewLine);

    /// <summary>
    /// Returns the exception messages all the way down the InnerException stack, but without any stack traces, separated by the "separator" parameter
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <param name="separator">Separator between messages. Use &lt;br/gt; for display in HTML, or Environment.NewLine (default) for plain text.</param>
    /// <returns></returns>
    public static string Messages(this Exception ex, string separator) {
      string msg = $"{ex.Message}{separator}";
      Exception innerException = ex.InnerException;
      while (innerException != null) {
        msg += $"{separator}{innerException.Message}{separator}";
        innerException = innerException.InnerException;
      }
      return msg;
    }


    /// <summary>
    /// Gets the innermost exception type in a stack
    /// </summary>
    /// <param name="ex">The top-level exception</param>
    /// <returns>The type of the innermost exception</returns>
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