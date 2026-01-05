using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Extensions {
  public static class EnumHelper {
    /// <summary>
    /// Returns a list of enum entries with their integer Ids and value names as string. Names are split by camel case, eg "MyEnumValue" becomes "My enum value".
    /// </summary>
    /// <typeparam name="T">The type of the enum</typeparam>
    /// <returns>A list of enum entries with their integer Ids and value names as string</returns>
    public static IEnumerable<(int id, string name)> GetValues<T>() where T : struct, IConvertible =>
      Enum.GetValues(typeof(T))
        .Cast<T>()
        .Select(f => (id: Convert.ToInt32(f), name: (Enum.GetName(typeof(T), f) ?? "").SplitCamelCase()));
  }
}