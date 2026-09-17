using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Validation;

internal static class RequestValidation
{
    public static void Text(List<string> errors, string? value, string name, int? maximum = null, bool required = false)
    {
        string? trimmed = value?.Trim();
        if (required && string.IsNullOrEmpty(trimmed))
        {
            errors.Add($"{name} is required.");
        }
        else if (maximum.HasValue && trimmed?.Length > maximum.Value)
        {
            errors.Add($"{name} must not exceed {maximum.Value} characters.");
        }
    }

    public static void Id(List<string> errors, Guid id)
    {
        if (id == Guid.Empty)
        {
            errors.Add("Id must not be empty.");
        }
    }

    public static void Paging(List<string> errors, PagedRequest request, params string[] sortFields)
    {
        Text(errors, request.Search, nameof(request.Search), 500);
        if (!string.IsNullOrWhiteSpace(request.SortBy) && !sortFields.Contains(request.SortBy, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("SortBy is not supported.");
        }

        if (!string.IsNullOrWhiteSpace(request.SortDirection)
            && !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("SortDirection must be asc or desc.");
        }
    }

    public static void Url(List<string> errors, string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")))
        {
            errors.Add($"{name} must be an absolute HTTP or HTTPS URL.");
        }
    }

    public static void Lists(List<string> errors, IReadOnlyCollection<string>? countries, IReadOnlyCollection<string>? formats, IReadOnlyCollection<string>? captions, IReadOnlyCollection<int>? genres)
    {
        if (countries is null || formats is null || captions is null || genres is null)
        {
            errors.Add("CountryCodes, FormatTags, CaptionOptions and GenreIds must be supplied as arrays.");
            return;
        }

        if (countries.Any(code => code is null || code.Trim().Length != 2 || !code.Trim().All(char.IsAsciiLetter)))
        {
            errors.Add("CountryCodes must contain two-letter codes.");
        }

        foreach (var (values, name) in new[] { (formats, "FormatTags"), (captions, "CaptionOptions") })
        {
            if (values.Any(value => string.IsNullOrWhiteSpace(value)
                || !value.Any(char.IsAsciiLetterOrDigit)
                || value.Trim().Any(character => !char.IsAsciiLetterOrDigit(character) && character != ' ' && character != '-' && character != '_')))
            {
                errors.Add($"{name} must contain descriptive tokens made from letters, digits, spaces, hyphens or underscores.");
            }
        }

        if (genres.Any(id => id <= 0))
        {
            errors.Add("GenreIds must be positive.");
        }
    }

    public static void ThrowIfInvalid(List<string> errors)
    {
        if (errors.Count != 0)
        {
            throw new CqrsValidationException(errors);
        }
    }
}
