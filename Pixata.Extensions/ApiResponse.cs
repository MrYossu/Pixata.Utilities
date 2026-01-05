using System;
using System.Threading.Tasks;
using static Pixata.Extensions.Yunit;

namespace Pixata.Extensions;

public record ApiResponse<T>(ApiResponseStates State, T? Data = default, string Message = "") {
  public static implicit operator bool(ApiResponse<T> a) =>
    a.State == ApiResponseStates.Success;

  public ApiResponse<TOut> Select<TOut>(Func<T, TOut> f) =>
    State == ApiResponseStates.Success
      ? new(State, f(Data!))
      : new(State, Message: Message);

  // This method is needed for sync queries. The one in the extension class below is for async queries
  public ApiResponse<TOut> SelectMany<TBind, TOut>(Func<T, ApiResponse<TBind>> bind, Func<T, TBind, TOut> project) {
    if (State != ApiResponseStates.Success) {
      return new ApiResponse<TOut>(State, Message: Message);
    }
    ApiResponse<TBind> mb = bind(Data!);
    return mb.State != ApiResponseStates.Success
      ? new ApiResponse<TOut>(mb.State, Message: mb.Message)
      : new ApiResponse<TOut>(ApiResponseStates.Success, project(Data!, mb.Data!));
  }

  public TOut Match<TOut>(Func<T, TOut> success, Func<string, TOut> failure) =>
    State == ApiResponseStates.Success
      ? success(Data!)
      : failure(Message);

  public Yunit Match(Action<T> success, Action<string> failure) {
    if (State == ApiResponseStates.Success) {
      success(Data!);
    } else {
      failure(Message);
    }
    return yunit;
  }
}

public static class ApiResponseExt {
  // This method allows us to compose async ApiResponse calls using the Linq query syntax
  public static Task<ApiResponse<TOut>> SelectMany<TFirst, TSecond, TOut>(this Task<ApiResponse<TFirst>> first, Func<TFirst, Task<ApiResponse<TSecond>>> selector, Func<TFirst, TSecond, TOut> projector) =>
    SelectManyAsync(first, selector, projector);

  private static async Task<ApiResponse<TOut>> SelectManyAsync<TFirst, TSecond, TOut>(Task<ApiResponse<TFirst>> first, Func<TFirst, Task<ApiResponse<TSecond>>> selector, Func<TFirst, TSecond, TOut> projector) {
    ApiResponse<TFirst> firstResult = await first;
    if (firstResult.State != ApiResponseStates.Success) {
      return new ApiResponse<TOut>(firstResult.State, Message: firstResult.Message);
    }
    ApiResponse<TSecond> secondResult = await selector(firstResult.Data!);
    if (secondResult.State != ApiResponseStates.Success) {
      return new ApiResponse<TOut>(secondResult.State, Message: secondResult.Message);
    }
    return new ApiResponse<TOut>(
      ApiResponseStates.Success,
      projector(firstResult.Data!, secondResult.Data!)
    );
  }

  /// <summary>
  /// Allows you to use a method that returns ApiResponse&lt;T&gt; in a Linq query expression with methods that return Task&lt;ApiResponse&lt;T&gt;&gt;
  /// </summary>
  /// <typeparam name="T">The type of the generic parameter for the ApiResponse</typeparam>
  /// <param name="ar">The ApiResponse&lt;T&gt; to be converted into a Task&lt;ApiResponse&lt;T&gt;&gt;</param>
  /// <returns></returns>
  public static Task<ApiResponse<T>> ToAsync<T>(this ApiResponse<T> ar) =>
    Task.FromResult(ar);
}

public enum ApiResponseStates {
  Loading,
  Success,
  NotFound,
  Failure,
  HttpFailure,
  ServiceUnavailable
}