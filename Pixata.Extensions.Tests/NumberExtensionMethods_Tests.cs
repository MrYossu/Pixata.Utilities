using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class NumberExtensionMethods_Tests {
    #region OrdinalSuffix

    [DataRow(1, "st", false)]
    [DataRow(2, "nd", false)]
    [DataRow(3, "rd", false)]
    [DataRow(4, "th", false)]
    [DataRow(5, "th", false)]
    [DataRow(6, "th", false)]
    [DataRow(7, "th", false)]
    [DataRow(8, "th", false)]
    [DataRow(9, "th", false)]
    [DataRow(10, "th", false)]
    [DataRow(11, "th", false)]
    [DataRow(12, "th", false)]
    [DataRow(13, "th", false)]
    [DataRow(14, "th", false)]
    [DataRow(15, "th", false)]
    [DataRow(16, "th", false)]
    [DataRow(17, "th", false)]
    [DataRow(18, "th", false)]
    [DataRow(19, "th", false)]
    [DataRow(20, "th", false)]
    [DataRow(21, "st", false)]
    [DataRow(22, "nd", false)]
    [DataRow(23, "rd", false)]
    [DataRow(24, "th", false)]
    [DataRow(25, "th", false)]
    [DataRow(26, "th", false)]
    [DataRow(27, "th", false)]
    [DataRow(28, "th", false)]
    [DataRow(29, "th", false)]
    [DataRow(30, "th", false)]
    [DataRow(31, "st", false)]
    [DataRow(32, "nd", false)]
    [DataRow(33, "rd", false)]
    [DataRow(34, "th", false)]
    [DataRow(35, "th", false)]
    [DataRow(36, "th", false)]
    [DataRow(37, "th", false)]
    [DataRow(38, "th", false)]
    [DataRow(39, "th", false)]
    [DataRow(40, "th", false)]
    [DataTestMethod]
    public void NumberExtensionMethods_OrdinalSuffix_IncludeNumber_False(int n, string suffix, bool includeNumber) =>
      Assert.AreEqual(suffix, n.OrdinalSuffix(includeNumber));

    [DataRow(1, "1st")]
    [DataRow(2, "2nd")]
    [DataRow(3, "3rd")]
    [DataRow(4, "4th")]
    [DataRow(5, "5th")]
    [DataRow(6, "6th")]
    [DataRow(7, "7th")]
    [DataRow(8, "8th")]
    [DataRow(9, "9th")]
    [DataRow(10, "10th")]
    [DataRow(11, "11th")]
    [DataRow(12, "12th")]
    [DataRow(13, "13th")]
    [DataRow(14, "14th")]
    [DataRow(15, "15th")]
    [DataRow(16, "16th")]
    [DataRow(17, "17th")]
    [DataRow(18, "18th")]
    [DataRow(19, "19th")]
    [DataRow(20, "20th")]
    [DataRow(21, "21st")]
    [DataRow(22, "22nd")]
    [DataRow(23, "23rd")]
    [DataRow(24, "24th")]
    [DataRow(25, "25th")]
    [DataRow(26, "26th")]
    [DataRow(27, "27th")]
    [DataRow(28, "28th")]
    [DataRow(29, "29th")]
    [DataRow(30, "30th")]
    [DataRow(31, "31st")]
    [DataRow(32, "32nd")]
    [DataRow(33, "33rd")]
    [DataRow(34, "34th")]
    [DataRow(35, "35th")]
    [DataRow(36, "36th")]
    [DataRow(37, "37th")]
    [DataRow(38, "38th")]
    [DataRow(39, "39th")]
    [DataRow(40, "40th")]
    [DataTestMethod]
    public void NumberExtensionMethods_OrdinalSuffix_DoNotIncludeNumber_False(int n, string suffix) =>
      Assert.AreEqual(suffix, n.OrdinalSuffix());

    #endregion

    #region DoubleToFraction

    // As tuples cannot be used in attribute values, we cheat and supply the expected value as a string, which we will convert to an integer tuple
    [DataRow(1, "1|1")]
    [DataRow(1.5, "3|2")]
    [DataRow(1.25, "5|4")]
    [DataRow(0.3333333333333333, "1|3")]
    [DataRow(3.5, "7|2")]
    [DataRow(5, "5|1")]
    [TestMethod]
    public void NumberExtensionMethods_DoubleToFraction(double value, string expected) {
      (int n, int d) expectedResult = (Convert.ToInt32(expected.Split('|')[0]), Convert.ToInt32(expected.Split('|')[1]));
      Assert.AreEqual(expectedResult, value.DoubleToFraction(0.001));
    }

    #endregion

    #region DoubleToProperFraction

    // As tuples cannot be used in attribute values, we cheat and supply the expected value as a string, which we will convert to an integer tuple
    [DataRow(1, "0|1|1")]
    [DataRow(1.5, "1|1|2")]
    [DataRow(1.25, "1|1|4")]
    [DataRow(0.3333333333333333, "0|1|3")]
    [DataRow(3.5, "3|1|2")]
    [DataRow(5, "5|0|1")]
    [TestMethod]
    public void NumberExtensionMethods_DoubleToProperFraction(double value, string expected) {
      (int, int, int) expectedResult = (Convert.ToInt32(expected.Split('|')[0]), Convert.ToInt32(expected.Split('|')[1]), Convert.ToInt32(expected.Split('|')[2]));
      Assert.AreEqual(expectedResult, value.DoubleToProperFraction(0.001));
    }

    #endregion

    #region DoubleToProperFractionString

    [DataRow(0, "0")]
    [DataRow(0.25, "1/4")]
    [DataRow(0.5, "1/2")]
    [DataRow(0.75, "3/4")]
    [DataRow(1, "1")]
    [DataRow(1.5, "1 1/2")]
    [DataRow(1.25, "1 1/4")]
    [DataRow(0.3333333333333333, "1/3")]
    [DataRow(3.5, "3 1/2")]
    [DataRow(5, "5")]
    [TestMethod]
    public void NumberExtensionMethods_DoubleToProperFractionString(double value, string expected) =>
      Assert.AreEqual(expected, value.DoubleToProperFractionString(0.001));

    #endregion

    #region ToPercentageString

    [DataRow(10, 100, 0, "10%")]
    [DataRow(10, 100, 1, "10%")]
    [DataRow(17.5, 100, 0, "18%")]
    [DataRow(17.5, 100, 1, "17.5%")]
    [DataTestMethod]
    public void NumberExtensionMethods_ToPercentageString(double qty, double max, int digits, string result) =>
      Assert.AreEqual(result, qty.ToPercentageString(max, digits));

    #endregion

    #region ToDurationString

    [DataRow(0, "zero")]
    [DataRow(1, "1 second")]
    [DataRow(5, "5 seconds")]
    [DataRow(30, "30 seconds")]
    [DataRow(59, "59 seconds")]
    [DataRow(60, "1 minute")]
    [DataRow(90, "1 minute 30 seconds")]
    [DataRow(120, "2 minutes")]
    [DataRow(125, "2 minutes 5 seconds")]
    [DataRow(3599, "59 minutes 59 seconds")]
    [DataRow(3600, "1 hour")]
    [DataRow(3601, "1 hour 1 second")]
    [DataRow(3660, "1 hour 1 minute")]
    [DataRow(3665, "1 hour 1 minute 5 seconds")]
    [DataRow(7200, "2 hours")]
    [DataRow(7205, "2 hours 5 seconds")]
    [DataRow(7275, "2 hours 1 minute 15 seconds")]
    [TestMethod]
    public void NumberExtensionMethods_ToDurationString(int value, string expected) =>
      Assert.AreEqual(expected, value.ToDurationString());

    #endregion

    #region ToFileSizeString

    [DataRow(0, 0, "0Kb")]
    [DataRow(1024, 0, "1Kb")]
    [DataRow(1024, 1, "1.0Kb")]
    [DataRow(1024 * 1024, 1, "1.0Mb")]
    [TestMethod]
    public void NumberExtensionMethods_ToFileSizeString(long value, int precision, string expected) =>
      Assert.AreEqual(expected, value.ToFileSizeString(precision));

    #endregion
  }
}