using System.Globalization;

namespace StoreApp.Domain.Common;

public static class Num
{
    /// <summary>
    /// Accepts "14.90", "14,90", "1 234,50", "1,234.50", "1.234,50".
    /// One lone separator is treated as decimal ("1,234" = 1.234); repeated ones as thousands ("1,234,567").
    /// </summary>
    public static bool TryParse(string? input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()); // also removes non-breaking spaces
        int comma = s.LastIndexOf(','), dot = s.LastIndexOf('.');

        if (comma >= 0 && dot >= 0)
        {
            var dec = comma > dot ? ',' : '.';
            var thousands = dec == ',' ? '.' : ',';
            s = s.Replace(thousands.ToString(), "").Replace(dec, '.');
        }
        else if (comma >= 0 || dot >= 0)
        {
            var sep = comma >= 0 ? ',' : '.';
            s = s.Count(c => c == sep) > 1 ? s.Replace(sep.ToString(), "") : s.Replace(sep, '.');
        }

        return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Culture-neutral text for &lt;input&gt; values, so the browser never gets "14,9" on one language and "14.9" on another.</summary>
    public static string ForInput(this decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}