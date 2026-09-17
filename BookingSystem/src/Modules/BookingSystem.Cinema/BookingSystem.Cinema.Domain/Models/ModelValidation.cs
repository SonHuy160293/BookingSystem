using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Validation;

namespace BookingSystem.Cinema.Domain.Models;

internal static class ModelValidation
{
    public static string RequiredText(string? value, int maxLength, string propertyName)
    {
        string? normalized = OptionalText(value, maxLength, propertyName);

        if (normalized is null)
        {
            throw Invalid(propertyName, $"{propertyName} is required.");
        }

        return normalized;
    }

    public static string? OptionalText(string? value, int? maxLength, string propertyName)
        => Guard.OptionalText(value, maxLength, propertyName);

    public static string Slug(string value, int maxLength)
    {
        string normalized = RequiredText(value, maxLength, nameof(Slug)).ToLowerInvariant();

        if (normalized[0] == '-' || normalized[^1] == '-' ||
            normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw Invalid(nameof(Slug), "Slug must contain letters, digits, or hyphens and begin and end with a letter or digit.");
        }

        return normalized;
    }

    public static void PositiveId(int value, string propertyName)
        => Guard.Require(value > 0, propertyName, $"{propertyName} must be positive.");

    public static void RequiredId(Guid value, string propertyName)
        => Guard.Require(value != Guid.Empty, propertyName, $"{propertyName} is required.");

    public static string[] CountryCodes(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var normalized = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string value in values)
        {
            string code = RequiredText(value, 2, nameof(Film.CountryCodes)).ToUpperInvariant();

            if (code.Length != 2 || !code.All(char.IsAsciiLetter))
            {
                throw Invalid(nameof(Film.CountryCodes), "Country codes must contain two letters.");
            }

            normalized.Add(code);
        }

        return [.. normalized];
    }

    public static string[] Tokens(IEnumerable<string> values, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var normalized = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string value in values)
        {
            string text = RequiredText(value, int.MaxValue, propertyName).ToLowerInvariant();
            string token = string.Join('_', text.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries));

            if (token.Length == 0 || token.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            {
                throw Invalid(propertyName, $"{propertyName} must contain descriptive tokens made from letters, digits, spaces, hyphens, or underscores.");
            }

            normalized.Add(token);
        }

        return [.. normalized];
    }

    public static ValidationException Invalid(string propertyName, string message)
        => Guard.Invalid(propertyName, message);
}
