using System;using System.Globalization;
public static class CurrencyInput {
 public static bool TryAmount(string text,out double amount){return Double.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out amount)&&amount>=1&&amount<=Int32.MaxValue&&amount==Math.Floor(amount);}
 public static bool TryRate(string text,out double rate){return Double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out rate)&&!Double.IsNaN(rate)&&!Double.IsInfinity(rate)&&rate>0;}
}
