using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Validation;

namespace BookingSystem.Order.Domain.Models;

internal static class ModelValidation
{
    public const decimal MaximumAmount = 999_999_999_999_999_999m;

    public static void Require(bool condition, string field, string message)
        => Guard.Require(condition, field, message);

    public static Guid Id(Guid value, string field)
        => Guard.Id(value, field);

    public static string Text(string value, int maximum, string field)
        => Guard.Text(value, maximum, field);

    public static string Code(string value, string field, int maximum = 64)
    {
        string code = Text(value, maximum, field);
        Require(code.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'), field, "A safe ASCII code is required.");
        return code;
    }

    public static string Hash(string value, string field)
        => Guard.Hash(value, field);

    public static decimal Money(decimal value, string field, bool positive = false)
    {
        Require(value >= 0 && value <= MaximumAmount && value == decimal.Truncate(value) && (!positive || value > 0),
            field, "Amount must contain bounded whole VND units.");
        return value;
    }

    public static void Owner(Guid? customerId, Guid? guestId, string? tokenHash)
    {
        Require(customerId.HasValue != guestId.HasValue, nameof(customerId), "Exactly one customer or guest owner is required.");
        if (customerId.HasValue)
        {
            Id(customerId.Value, nameof(customerId));
            Require(tokenHash is null, nameof(tokenHash), "Customer ownership cannot contain a guest credential.");
        }
        else
        {
            Id(guestId!.Value, nameof(guestId));
            Hash(tokenHash!, nameof(tokenHash));
        }
    }

    public static void Category(OrderSeatCategory category)
        => Guard.DefinedEnum(category, nameof(category), "Seat category is invalid.");
}
