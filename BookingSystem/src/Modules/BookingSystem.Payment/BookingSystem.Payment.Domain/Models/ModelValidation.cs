using BookingSystem.SharedKernel.Validation;

namespace BookingSystem.Payment.Domain.Models;

internal static class ModelValidation
{
    public const decimal MaximumAmount = 999_999_999_999_999_999m;

    public static void Require(bool condition, string field, string message)
        => Guard.Require(condition, field, message);

    public static Guid Id(Guid value, string field)
        => Guard.Id(value, field);

    public static string Text(string value, int maximum, string field)
        => Guard.Text(value, maximum, field, rejectControlCharacters: true);

    public static string Code(string value, string field, int maximum = 64)
    {
        string code = Text(value, maximum, field);
        Require(code.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'), field, "A safe ASCII code is required.");
        return code;
    }

    public static string Hash(string value, string field)
        => Guard.Hash(value, field);

    public static decimal Money(decimal value, string field)
    {
        Require(value > 0 && value <= MaximumAmount && value == decimal.Truncate(value), field, "Positive bounded whole VND units are required.");
        return value;
    }

    public static void Lease(Guid token, DateTimeOffset now, DateTimeOffset until)
    {
        Id(token, nameof(token));
        Require(until > now, nameof(until), "Lease deadline must follow the current time.");
    }

    public static void OwnLease(Guid token, Guid? actual, DateTimeOffset? until, DateTimeOffset now)
        => Require(token != Guid.Empty && token == actual && until.HasValue && until.Value > now, nameof(token), "A current matching lease is required.");
}
