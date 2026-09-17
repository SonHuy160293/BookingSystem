using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class ReservedSeat : Entity<Guid>
{
    private ReservedSeat() { }

    public Guid ReservationId { get; private set; }
    public Guid ShowtimeId { get; private set; }
    public Guid ShowtimeSeatId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public SeatReservation Reservation { get; private set; } = null!;
    public ShowtimeSeat ShowtimeSeat { get; private set; } = null!;

    internal static ReservedSeat Create(SeatReservation reservation, ShowtimeSeat seat)
        => new()
        {
            Id = Guid.CreateVersion7(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            ShowtimeId = reservation.ShowtimeId,
            ShowtimeSeatId = seat.Id,
            ShowtimeSeat = seat,
            UnitPrice = seat.UnitPrice
        };
}
