using System.Text;

namespace Arte.Sport.Core.Identity;

public static class Mobile
{
    public static bool TryNormalize(string? raw, out string mobile)
    {
        mobile = "";
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var digits = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
        {
            var c = ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                _ => ch,
            };
            if (c is >= '0' and <= '9') digits.Append(c);
        }

        var s = digits.ToString();
        if (s.Length == 10 && s.StartsWith('9')) s = "0" + s;
        if (s.Length != 11 || !s.StartsWith("09", StringComparison.Ordinal)) return false;
        mobile = s;
        return true;
    }
}
