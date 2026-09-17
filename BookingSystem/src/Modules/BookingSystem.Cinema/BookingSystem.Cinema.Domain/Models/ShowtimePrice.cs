using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class ShowtimePrice : Entity<Guid>
{
    private const decimal MaximumAmount = 999_999_999_999_999_999m;

    private ShowtimePrice() { }

    private ShowtimePrice(Guid id, Showtime showtime, SeatCategory category, decimal amount)
        : base(id)
    {
        ShowtimeId = showtime.Id;
        Showtime = showtime;
        SeatCategory = category;
        ChangeAmount(amount);
    }

    public Guid ShowtimeId { get; private set; }
    public SeatCategory SeatCategory { get; private set; }
    public decimal Amount { get; private set; }
    public Showtime Showtime { get; private set; } = null!;

    internal static ShowtimePrice Create(Showtime showtime, SeatCategory category, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(showtime);
        ModelValidation.RequiredId(showtime.Id, nameof(ShowtimeId));
        ValidateCategory(category);

        return new ShowtimePrice(Guid.CreateVersion7(), showtime, category, amount);
    }

    internal static void ValidateCategory(SeatCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw ModelValidation.Invalid(nameof(SeatCategory), "SeatCategory is invalid.");
        }
    }

    internal void ChangeAmount(decimal amount)
    {
        if (amount < 0 || amount > MaximumAmount)
        {
            throw ModelValidation.Invalid(nameof(Amount), "Amount must be between 0 and 999999999999999999 VND.");
        }

        if (amount != decimal.Truncate(amount))
        {
            throw ModelValidation.Invalid(nameof(Amount), "Amount must contain whole VND currency units.");
        }

        Amount = amount;
    }
}
