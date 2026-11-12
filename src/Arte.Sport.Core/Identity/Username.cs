namespace Arte.Sport.Core.Identity;

public static class Username
{
    public static bool TryNormalize(string? raw, out string username)
    {
        username = "";
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var value = raw.Trim().ToLowerInvariant();
        if (value.Length is < 3 or > 32) return false;
        foreach (var ch in value)
        {
            if (ch is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_' or '-'))
                return false;
        }
        username = value;
        return true;
    }
}
