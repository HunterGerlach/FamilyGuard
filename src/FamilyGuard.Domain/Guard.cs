namespace FamilyGuard.Domain;

internal static class Guard
{
    public static string NotBlank(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    public static IReadOnlyList<T> Snapshot<T>(IEnumerable<T>? values)
    {
        return values is null ? [] : values.ToArray();
    }
}
