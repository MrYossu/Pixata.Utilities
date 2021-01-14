using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Extensions {
  public static class EnumHelper {
    public static IEnumerable<(int id, string name)> GetValues<T>() where T : struct, IConvertible =>
      Enum.GetValues(typeof(T))
        .Cast<T>()
        .Select(f => (id: Convert.ToInt32(f), name: (Enum.GetName(typeof(T), f) ?? "").SplitCamelCase()));
  }
}