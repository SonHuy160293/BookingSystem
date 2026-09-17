using System.Globalization;
using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class OrderItem : Entity<Guid>
{
    private OrderItem() { }

    public Guid OrderId { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid ReservedSeatId { get; private set; }
    public Guid ShowtimeId { get; private set; }
    public Guid ShowtimeSeatId { get; private set; }
    public Guid SeatLayoutSeatId { get; private set; }
    public string Row { get; private set; } = null!;
    public int Column { get; private set; }
    public string Label => Row + Column.ToString(CultureInfo.InvariantCulture);
    public OrderSeatCategory Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public BookingOrder Order { get; private set; } = null!;
    public ReservedSeat ReservedSeat { get; private set; } = null!;
    public ShowtimeSeat ShowtimeSeat { get; private set; } = null!;

    internal static OrderItem Create(BookingOrder order, ReservedSeat reserved, ShowtimeSeat seat)
    {
        ModelValidation.Require(reserved.ReservationId == order.ReservationId && reserved.ShowtimeId == order.ShowtimeId
            && seat.Id == reserved.ShowtimeSeatId && seat.ShowtimeId == order.ShowtimeId,
            nameof(reserved), "Reservation and inventory positions must match the order.");
        return new OrderItem
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            Order = order,
            ReservationId = reserved.ReservationId,
            ReservedSeatId = reserved.Id,
            ReservedSeat = reserved,
            ShowtimeId = seat.ShowtimeId,
            ShowtimeSeatId = seat.Id,
            ShowtimeSeat = seat,
            SeatLayoutSeatId = seat.SeatLayoutSeatId,
            Row = seat.Row,
            Column = seat.Column,
            Category = seat.Category,
            UnitPrice = reserved.UnitPrice
        };
    }
}
