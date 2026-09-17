using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class SeatLayoutSeat : Entity<Guid>
{
    private SeatLayoutSeat() { }

    private SeatLayoutSeat(Guid id, SeatLayoutVersion layout, Seat seat, SeatCategory category)
        : base(id)
    {
        SeatLayoutVersionId = layout.Id;
        SeatLayoutVersion = layout;
        SeatId = seat.Id;
        Seat = seat;
        ChangeCategory(category);
    }

    public Guid SeatLayoutVersionId { get; private set; }
    public Guid SeatId { get; private set; }
    public SeatCategory Category { get; private set; }
    public SeatLayoutVersion SeatLayoutVersion { get; private set; } = null!;
    public Seat Seat { get; private set; } = null!;

    internal static SeatLayoutSeat Create(SeatLayoutVersion layout, Seat seat, SeatCategory category)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(seat);
        ModelValidation.RequiredId(layout.Id, nameof(SeatLayoutVersionId));
        ModelValidation.RequiredId(seat.Id, nameof(SeatId));

        return new SeatLayoutSeat(Guid.CreateVersion7(), layout, seat, category);
    }

    internal void ChangeCategory(SeatCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw ModelValidation.Invalid(nameof(Category), "Category is invalid.");
        }

        Category = category;
    }
}
