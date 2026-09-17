using System.Globalization;
using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class ShowtimeSeat : Entity<Guid>
{
    private ShowtimeSeat() { }

    public Guid ShowtimeId { get; private set; }
    public Guid SeatLayoutSeatId { get; private set; }
    public Guid SeatId { get; private set; }
    public string Row { get; private set; } = null!;
    public int Column { get; private set; }
    public string Label => Row + Column.ToString(CultureInfo.InvariantCulture);
    public OrderSeatCategory Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public SeatAllocationStatus Status { get; private set; }
    public Guid? CurrentReservationId { get; private set; }
    public Guid? CurrentOrderId { get; private set; }
    public DateTimeOffset? HoldExpiresAt { get; private set; }
    public ShowtimeInventory Showtime { get; private set; } = null!;
    public SeatReservation? CurrentReservation { get; private set; }
    public BookingOrder? CurrentOrder { get; private set; }

    internal static ShowtimeSeat Create(ShowtimeInventory showtime, Guid layoutSeatId, Guid seatId,
        string row, int column, OrderSeatCategory category, decimal unitPrice)
    {
        string normalizedRow = ModelValidation.Text(row, 1, nameof(row));
        ModelValidation.Require(char.IsAsciiLetter(normalizedRow[0]) && column is >= 1 and <= 100, nameof(row), "Position must be A1 through Z100.");
        ModelValidation.Category(category);
        return new ShowtimeSeat
        {
            Id = Guid.CreateVersion7(),
            ShowtimeId = showtime.Id,
            Showtime = showtime,
            SeatLayoutSeatId = ModelValidation.Id(layoutSeatId, nameof(layoutSeatId)),
            SeatId = ModelValidation.Id(seatId, nameof(seatId)),
            Row = normalizedRow.ToUpperInvariant(),
            Column = column,
            Category = category,
            UnitPrice = ModelValidation.Money(unitPrice, nameof(unitPrice)),
            Status = SeatAllocationStatus.Available
        };
    }

    internal bool IsHeldBy(Guid reservationId) => Status == SeatAllocationStatus.Held && CurrentReservationId == reservationId;

    internal void Hold(SeatReservation reservation)
    {
        ModelValidation.Require(Status == SeatAllocationStatus.Available && reservation.ShowtimeId == ShowtimeId,
            nameof(Status), "Seat is unavailable.");
        CurrentReservationId = reservation.Id;
        CurrentReservation = reservation;
        HoldExpiresAt = reservation.ExpiresAt;
        Status = SeatAllocationStatus.Held;
    }

    internal void Release(Guid reservationId)
    {
        ModelValidation.Require(IsHeldBy(reservationId), nameof(reservationId), "Reservation does not own this seat.");
        Status = SeatAllocationStatus.Available;
        CurrentReservationId = null;
        CurrentReservation = null;
        CurrentOrderId = null;
        CurrentOrder = null;
        HoldExpiresAt = null;
    }

    internal void MarkSold(Guid reservationId, BookingOrder order)
    {
        ModelValidation.Require(IsHeldBy(reservationId) && order.ShowtimeId == ShowtimeId,
            nameof(reservationId), "Reservation does not own this seat.");
        CurrentOrderId = order.Id;
        CurrentOrder = order;
        HoldExpiresAt = null;
        Status = SeatAllocationStatus.Sold;
    }

    internal void SetBlocked(bool blocked) => Status = blocked ? SeatAllocationStatus.Blocked : SeatAllocationStatus.Available;
}
