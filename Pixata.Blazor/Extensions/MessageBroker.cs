using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Pixata.Blazor.Extensions;

public class MessageBroker {
  private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();

  public async Task SendMessage<T>(T data) {
    try {
      if (_handlers.TryGetValue(typeof(T), out Delegate? handler)) {
        if (handler is Func<T, Task> asyncHandler) {
          await asyncHandler(data);
        }
      }
    } catch (Exception ex) {
      Console.WriteLine($"Error processing message of type {typeof(T).Name} - {ex.Message}");
      throw;
    }
  }

  public void Subscribe<T>(Func<T, Task> handler) =>
    _handlers.AddOrUpdate(typeof(T), handler, (_, existingHandler) => {
      if (existingHandler is Func<T, Task> existing) {
        return (Func<T, Task>)(async data => {
          await existing(data);
          await handler(data);
        });
      }
      return handler;
    });

  public void Unsubscribe<T>(Func<T, Task> handler) {
    if (_handlers.TryGetValue(typeof(T), out Delegate? currentHandler) && currentHandler is Func<T, Task> existingHandler) {
      if (existingHandler.Equals(handler)) {
        // This is the only handler, so remove it completely
        _handlers.TryRemove(typeof(T), out _);
      } else if (existingHandler.GetInvocationList().Length > 1) {
        // There are multiple handlers, so remove just this one
        Delegate? newDelegate = Delegate.Remove(existingHandler, handler);
        if (newDelegate is not null) {
          _handlers.TryUpdate(typeof(T), newDelegate, currentHandler);
        }
      }
    }
  }
}

public class MessageBrokerInstance {
  public readonly MessageBroker Broker = new();
}