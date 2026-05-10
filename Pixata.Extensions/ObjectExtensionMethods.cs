using System.Reflection;

namespace Pixata.Extensions;

public static class ObjectExtensionMethods {
  /// <summary>
  /// Returns a shallow clone of an object
  /// </summary>
  /// <typeparam name="T">The type of the object to be cloned</typeparam>
  /// <param name="source">The object to be cloned</param>
  /// <returns>A shallow clone of the object. Uses reflection, but despite all the myths about this being slow, doing 10 million clones only took about 300ms longer than a reflection-free (and way more complex) version, so I went for simplicity.</returns>
  public static T Clone<T>(this T source) where T : class =>
    (T)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(source, null)!;
}