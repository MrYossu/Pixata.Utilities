namespace Pixata.Functional {
  public static class Functions {
    // https://stackoverflow.com/a/14071657/706346
    public static T Identity<T>(T value) =>
      value;

    public static T0 Default<T0>() =>
      default(T0);

    public static T0 Default<T1, T0>(T1 value1) =>
      default(T0);
    /* Put as many overloads as you want */

    /* Some other potential methods */
    public static bool IsNull<T>(T entity) where T : class =>
      entity == null;

    public static bool IsNonNull<T>(T entity) where T : class =>
      entity != null;

    /* Put as many overloads for True and False as you want */
    public static bool True<T>(T entity) =>
      true;

    public static bool False<T>(T entity) =>
      false;
  }
}