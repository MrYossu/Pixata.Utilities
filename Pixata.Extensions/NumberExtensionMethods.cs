using System;

namespace Pixata.Extensions {
  public static class NumberExtensionMethods {
    /// <summary>
    /// Returns the ordinal suffix for a number, eg "st" for 1, "nd" for 2, "rd" for 3 and so on. Useful for formatting dates
    /// </summary>
    /// <param name="n">An integer</param>
    /// <param name="includeNumber">If true (9)default) then includes the number in the return value, eg "9th". If false, returns just the ordinal suffix, eg "th"</param>
    /// <returns>The ordinal suffix for the integer, optionally with the integer itself</returns>
    public static string OrdinalSuffix(this int n, bool includeNumber = true) =>
      (includeNumber ? n : "") + n switch {
        11 or 12 or 13 => "th",
        _ when n.ToString().EndsWith("1") => "st",
        _ when n.ToString().EndsWith("2") => "nd",
        _ when n.ToString().EndsWith("3") => "rd",
        _ => "th"
      };

    /// <summary>
    /// Converts a double to its proper fractional representation, eg 3.5 is converted to (3, 2, 2), ie 3 1/2
    /// </summary>
    /// <param name="value">The number to be converted</param>
    /// <param name="accuracy">The accuracy of the conversion. Where value doesn't have a neat fractional representation, this determines how far we go trying to find one</param>
    /// <returns></returns>
    public static (int, int, int) DoubleToProperFraction(this double value, double accuracy) {
      (int n, int d) = DoubleToFraction(value, accuracy);
      if (n <= d) {
        return (0, n, d);
      }
      return (n / d, n - (n / d) * d, d);
    }

    public static string DoubleToProperFractionString(this double value, double accuracy) =>
      value.DoubleToProperFraction(accuracy) switch {
        (int n1, 0, _) => $"{n1}",
        (0, int n2, int n3) when n2 == n3 => $"1",
        (0, int n2, int n3) => $"{n2}/{n3}",
        (int n1, int n2, int n3) => $"{n1} {n2}/{n3}"
      };

    /// <summary>
    /// Converts a double to its improper fractional representation, eg 3.5 is converted to (7, 2), ie 7/2. Slightly modified from https://stackoverflow.com/a/32903747/706346
    /// </summary>
    /// <param name="value"></param>
    /// <param name="accuracy"></param>
    /// <returns></returns>
    public static (int, int) DoubleToFraction(this double value, double accuracy) {
      if (accuracy <= 0.0 || accuracy >= 1.0) {
        throw new ArgumentOutOfRangeException("accuracy", "Must be > 0 and < 1.");
      }

      int sign = Math.Sign(value);

      if (sign == -1) {
        value = Math.Abs(value);
      }

      // Accuracy is the maximum relative error; convert to absolute maxError
      double maxError = sign == 0 ? accuracy : value * accuracy;

      int n = (int)Math.Floor(value);
      value -= n;

      if (value < maxError) {
        return (sign * n, 1);
      }

      if (1 - maxError < value) {
        return (sign * (n + 1), 1);
      }

      // The lower fraction is 0/1
      int lower_n = 0;
      int lower_d = 1;

      // The upper fraction is 1/1
      int upper_n = 1;
      int upper_d = 1;

      while (true) {
        // The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
        int middle_n = lower_n + upper_n;
        int middle_d = lower_d + upper_d;

        if (middle_d * (value + maxError) < middle_n) {
          // real + error < middle : middle is our new upper
          upper_n = middle_n;
          upper_d = middle_d;
        } else if (middle_n < (value - maxError) * middle_d) {
          // middle < real - error : middle is our new lower
          lower_n = middle_n;
          lower_d = middle_d;
        } else {
          // Middle is our best fraction
          return ((n * middle_d + middle_n) * sign, middle_d);
        }
      }
    }

    /// <summary>
    /// Converts a number into the string representation of it as a percentage of a maximum.
    /// </summary>
    /// <param name="qty">The number that is to be a percentage. Can be an int or a double</param>
    /// <param name="max">The maximum value. Can be an int or a double</param>
    /// <param name="digits">The number of decimal digits to be included in the result, default is zero. Eg 15.ToPercentageString(100) would return "15%", 17.5.ToPercentageString(100) would return "17%" and 17.5.ToPercentageString(100, 1) would return "17.5%"</param>
    /// <returns></returns>
    public static string ToPercentageString(this int qty, int max, int digits = 0) =>
      ToPercentageString((double)qty, (double)max, digits);

    public static string ToPercentageString(this int qty, double max, int digits = 0) =>
      ToPercentageString((double)qty, max, digits);

    public static string ToPercentageString(this double qty, int max, int digits = 0) =>
      ToPercentageString(qty, (double)max, digits);

    public static string ToPercentageString(this double qty, double max, int digits = 0) =>
      $"{Math.Round(100 * qty / max, digits)}%";
  }
}