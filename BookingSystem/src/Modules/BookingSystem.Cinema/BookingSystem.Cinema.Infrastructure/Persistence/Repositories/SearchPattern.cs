namespace BookingSystem.Cinema.Infrastructure.Persistence.Repositories;

internal static class SearchPattern
{
    public static string Contains(string value)
        => "%" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
}
