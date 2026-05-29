namespace FirebirdTraceParser.Core.Parsing.Utils;

public static class StringPool
{
    private static readonly Dictionary<string, string> Pool = new(StringComparer.Ordinal);

    private static readonly Lock Lock = new();

    public static string Intern(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        lock (Lock)
        {
            if (Pool.TryGetValue(value, out var existing))
                return existing;

            Pool[value] = value;
            return value;
        }
    }

    public static void Reset()
    {
        lock (Lock)
            Pool.Clear();
    }
}