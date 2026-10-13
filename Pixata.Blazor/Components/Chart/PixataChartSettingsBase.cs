using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base of the child components that configure a <see cref="PixataChart"/> (its series, axes, title, legend and so on).
/// They render nothing themselves: they attach themselves to their parent when they are created, and ask the chart to redraw
/// when one of their parameters changes
/// </summary>
public abstract class PixataChartSettingsBase : ComponentBase, IDisposable {
  private static readonly ConcurrentDictionary<Type, PropertyInfo[]> SignatureProperties = new();

  [CascadingParameter]
  protected PixataChart? CascadedChart { get; set; }

  private object?[]? _signature;
  private bool _attached;

  internal PixataChart? Chart => CascadedChart;

  internal abstract void Attach();

  internal abstract void Detach();

  protected override void OnInitialized() {
    Attach();
    _attached = true;
    Chart?.Invalidate();
  }

  protected override void OnParametersSet() {
    object?[] signature = Signature();
    if (_signature is not null && !Same(_signature, signature)) {
      Chart?.Invalidate();
    }
    _signature = signature;
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
  }

  /// <summary>
  /// Renders <paramref name="content"/> with this component cascaded to it, so the child settings inside it can find it
  /// </summary>
  internal void RenderCascading<T>(RenderTreeBuilder builder, T value, RenderFragment? content) {
    if (content is null) {
      return;
    }
    builder.OpenComponent<CascadingValue<T>>(0);
    builder.AddComponentParameter(1, nameof(CascadingValue<T>.Value), value);
    builder.AddComponentParameter(2, nameof(CascadingValue<T>.IsFixed), true);
    builder.AddComponentParameter(3, nameof(CascadingValue<T>.ChildContent), content);
    builder.CloseComponent();
  }

  /// <summary>
  /// The values of the parameters that affect how the chart is drawn. Event callbacks are left out, as they are only read
  /// when the event happens, and so is the child content, as the settings inside it report their own changes. Templates are
  /// kept, as the chart uses them while it is drawn (a label template, say). Razor creates a new delegate every time the page
  /// renders, but delegates compare equal when they call the same method on the same object, so a template only counts as
  /// changed when it really is a different one (or one that captured different values)
  /// </summary>
  private object?[] Signature() =>
    SignatureProperties.GetOrAdd(GetType(), type => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null && p.Name != "ChildContent" && !p.PropertyType.Name.StartsWith("EventCallback"))
        .OrderBy(p => p.Name)
        .ToArray())
      .Select(p => p.GetValue(this))
      .ToArray();

  private static bool Same(object?[] a, object?[] b) {
    for (int i = 0; i < a.Length; i++) {
      object? x = a[i];
      object? y = b[i];
      bool same = x switch {
        null => y is null,
        string or ValueType or Delegate => x.Equals(y),
        object[] array => y is object[] other && array.SequenceEqual(other),
        _ => ReferenceEquals(x, y)
      };
      if (!same) {
        return false;
      }
    }
    return true;
  }

  public void Dispose() {
    if (_attached) {
      Detach();
      _attached = false;
      Chart?.Invalidate();
    }
    GC.SuppressFinalize(this);
  }
}
