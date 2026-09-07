using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Tests {
  /// <summary>
  /// Lets tests create a PersistentComponentState, give it existing state to be read, and run the persisting callbacks
  /// </summary>
  /// <remarks>
  /// PersistentComponentState has no public constructor, and no public way either to load existing state or to fire
  /// the persisting callbacks, so all of that has to be done by reflection. That makes this class fragile if the
  /// internals of ASP.NET Core change, but there is no supported alternative, and the behaviour being tested (what
  /// PersistentStateHelper does when a persisted value can't be read) can only be tested against the real thing
  /// </remarks>
  internal sealed class PersistentComponentStateHarness {
    private static readonly Type StateType = typeof(PersistentComponentState);
    private static readonly Type RegistrationType = StateType.Assembly.GetType("Microsoft.AspNetCore.Components.PersistComponentStateRegistration")!;

    private readonly IList _registrations;

    /// <summary>
    /// The state that has been persisted by PersistAsJson, ie what would be sent to the client
    /// </summary>
    public Dictionary<string, byte[]> Persisted { get; } = new();

    public PersistentComponentState State { get; }

    public PersistentComponentStateHarness(IDictionary<string, byte[]>? existingState = null) {
      _registrations = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RegistrationType))!;
      State = (PersistentComponentState)StateType
        .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single()
        .Invoke(new object[] { Persisted, _registrations });
      if (existingState is not null) {
        StateType
          .GetMethod("InitializeExistingState", BindingFlags.Instance | BindingFlags.NonPublic)!
          .Invoke(State, new object[] { existingState });
      }
    }

    /// <summary>
    /// Runs everything that was registered with RegisterOnPersisting, as the framework does when the prerender ends
    /// </summary>
    public async Task RunPersistCallbacks() {
      PropertyInfo persistingState = StateType.GetProperty("PersistingState", BindingFlags.Instance | BindingFlags.NonPublic)!;
      PropertyInfo callback = RegistrationType.GetProperty("Callback")!;
      persistingState.SetValue(State, true);
      try {
        foreach (object registration in _registrations) {
          await ((Func<Task>)callback.GetValue(registration)!)();
        }
      } finally {
        persistingState.SetValue(State, false);
      }
    }
  }
}
