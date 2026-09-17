using System.Net.Mail;
using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class BookingOrder : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];
    private readonly List<Ticket> _tickets = [];
    private readonly List<OrderRefundRequest> _refundRequests = [];

    private BookingOrder() { }

    public Guid ReservationId { get; private set; }
    public Guid ShowtimeId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? GuestId { get; private set; }
    public string? GuestAccessTokenHash { get; private set; }
    public string ContactEmail { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public int ItemCount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public BookingOrderStatus Status { get; private set; }
    public Guid? PaymentId { get; private set; }
    public Guid? AcceptedPaymentAttemptId { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public SeatReservation Reservation { get; private set; } = null!;
    public ShowtimeInventory Showtime { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();
    public IReadOnlyCollection<OrderRefundRequest> RefundRequests => _refundRequests.AsReadOnly();

    public static BookingOrder Create(ShowtimeInventory inventory, SeatReservation reservation, string contactEmail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(reservation);
        reservation.EnsureConvertible(now);
        ModelValidation.Require(reservation.ShowtimeId == inventory.Id && (inventory.Status is InventoryStatus.OnSale or InventoryStatus.Closed)
            && now < inventory.StartsAt, nameof(reservation), "Reservation is not eligible for order creation.");
        string email = ModelValidation.Text(contactEmail, 320, nameof(contactEmail));
        ModelValidation.Require(MailAddress.TryCreate(email, out var address) && string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase) && !email.Any(char.IsControl), nameof(contactEmail), "A contact email is required.");
        ModelValidation.Money(reservation.TotalAmount, nameof(reservation.TotalAmount), positive: true);
        ShowtimeSeat[] seats = reservation.Seats.Select(s => inventory.FindSeat(s.ShowtimeSeatId)).ToArray();
        ModelValidation.Require(seats.All(s => s.IsHeldBy(reservation.Id)), nameof(reservation), "Reservation must still own every seat.");
        var order = new BookingOrder
        {
            Id = Guid.CreateVersion7(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            ShowtimeId = inventory.Id,
            Showtime = inventory,
            TenantId = inventory.TenantId,
            CustomerId = reservation.CustomerId,
            GuestId = reservation.GuestId,
            GuestAccessTokenHash = reservation.GuestAccessTokenHash,
            ContactEmail = email,
            TotalAmount = reservation.TotalAmount,
            ItemCount = reservation.SeatCount,
            Status = BookingOrderStatus.AwaitingPayment
        };
        foreach (var reserved in reservation.Seats)
        {
            order._items.Add(OrderItem.Create(order, reserved, inventory.FindSeat(reserved.ShowtimeSeatId)));
        }

        ModelValidation.Require(order._items.Sum(item => item.UnitPrice) == order.TotalAmount, nameof(reservation), "Reservation price snapshot is incomplete.");
        reservation.MarkConverted(now);
        return order;
    }

    public void AssignPayment(Guid paymentId)
    {
        ModelValidation.Id(paymentId, nameof(paymentId));
        ModelValidation.Require(Status == BookingOrderStatus.AwaitingPayment && (!PaymentId.HasValue || PaymentId == paymentId),
            nameof(paymentId), "Payment cannot be reassigned.");
        PaymentId = paymentId;
    }

    public BookingConfirmationOutcome Confirm(ShowtimeInventory inventory, SeatReservation reservation,
        Guid paymentId, Guid attemptId, decimal amount, string currencyCode,
        IEnumerable<TicketCredentials> credentials, DateTimeOffset now)
    {
        EnsureReferences(inventory, reservation);
        ModelValidation.Id(paymentId, nameof(paymentId));
        ModelValidation.Id(attemptId, nameof(attemptId));
        ModelValidation.Require(amount == TotalAmount && currencyCode == CurrencyCode, nameof(amount), "Verified payment must match the order total and currency.");
        ModelValidation.Require(!PaymentId.HasValue || PaymentId == paymentId, nameof(paymentId), "Payment belongs to another checkout.");

        if (AcceptedPaymentAttemptId == attemptId)
        {
            return BookingConfirmationOutcome.AlreadyConfirmed;
        }

        ModelValidation.Require(reservation.Seats.Count == reservation.SeatCount
            && reservation.Seats.All(reserved => inventory.Seats.Any(seat => seat.Id == reserved.ShowtimeSeatId)),
            nameof(reservation), "Load the complete reservation and its inventory seats before confirmation.");

        if (Status != BookingOrderStatus.AwaitingPayment || !inventory.CanFulfill(reservation, now))
        {
            OrderRefundReason reason = Status == BookingOrderStatus.Confirmed ? OrderRefundReason.DuplicateCharge
                : inventory.Status == InventoryStatus.Cancelled ? OrderRefundReason.ShowtimeCancellation : OrderRefundReason.LatePayment;
            AddRefund(paymentId, attemptId, reason, now);
            return BookingConfirmationOutcome.RefundRequired;
        }

        ArgumentNullException.ThrowIfNull(credentials);
        ModelValidation.Require(_items.Count == ItemCount && ItemCount == reservation.SeatCount, nameof(Items), "The complete order item collection is required.");
        TicketCredentials[] tokens = credentials.ToArray();
        ModelValidation.Require(tokens.Length == ItemCount && tokens.All(token => token is not null)
            && tokens.Select(token => token.OrderItemId).ToHashSet().SetEquals(_items.Select(item => item.Id)),
            nameof(credentials), "Exactly one credential set per order item is required.");
        Ticket[] tickets = _items.Select(item => Ticket.Create(item, tokens.Single(token => token.OrderItemId == item.Id), now)).ToArray();
        ModelValidation.Require(tickets.Select(t => t.TicketCode).Distinct(StringComparer.Ordinal).Count() == ItemCount
            && tickets.Select(t => t.QrTokenHash).Distinct(StringComparer.Ordinal).Count() == ItemCount,
            nameof(credentials), "Ticket credentials must be distinct.");
        inventory.Sell(reservation, this, now);
        PaymentId = paymentId;
        AcceptedPaymentAttemptId = attemptId;
        ConfirmedAt = now.ToUniversalTime();
        Status = BookingOrderStatus.Confirmed;
        _tickets.AddRange(tickets);
        return BookingConfirmationOutcome.Confirmed;
    }

    public void Expire(ShowtimeInventory inventory, SeatReservation reservation, DateTimeOffset now)
    {
        EnsureReferences(inventory, reservation);
        if (Status == BookingOrderStatus.Expired)
        {
            return;
        }

        ModelValidation.Require(Status == BookingOrderStatus.AwaitingPayment && now >= reservation.ExpiresAt,
            nameof(Status), "Only an unpaid expired order can expire.");
        inventory.ReleaseReservation(reservation, now, expired: true);
        Status = BookingOrderStatus.Expired;
    }

    public void CancelForShowtime(ShowtimeInventory inventory, SeatReservation reservation, DateTimeOffset now)
    {
        EnsureReferences(inventory, reservation);
        ModelValidation.Require(inventory.Status == InventoryStatus.Cancelled, nameof(inventory), "Showtime inventory must already be cancelled.");
        if (Status is BookingOrderStatus.Cancelled or BookingOrderStatus.Expired)
        {
            return;
        }

        string reason = ModelValidation.Code(inventory.CancellationReasonCode!, nameof(CancellationReasonCode));
        if (Status == BookingOrderStatus.Confirmed)
        {
            ModelValidation.Require(_tickets.Count == ItemCount && _tickets.All(t => now >= t.IssuedAt && (!t.UsedAt.HasValue || now >= t.UsedAt.Value)),
                nameof(Tickets), "The complete issued ticket collection and a valid cancellation time are required.");
            AddRefund(PaymentId!.Value, AcceptedPaymentAttemptId!.Value, OrderRefundReason.ShowtimeCancellation, now);
            foreach (var ticket in _tickets)
            {
                ticket.Void(reason, now);
            }
        }
        else
        {
            inventory.ReleaseReservation(reservation, now, expired: false);
        }

        Status = BookingOrderStatus.Cancelled;
        CancelledAt = now.ToUniversalTime();
        CancellationReasonCode = reason;
    }

    private void EnsureReferences(ShowtimeInventory inventory, SeatReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(reservation);
        ModelValidation.Require(inventory.Id == ShowtimeId && reservation.Id == ReservationId && reservation.ShowtimeId == ShowtimeId,
            nameof(reservation), "Inventory, order and reservation must match.");
    }

    private void AddRefund(Guid paymentId, Guid attemptId, OrderRefundReason reason, DateTimeOffset now)
    {
        var existing = _refundRequests.Find(request => request.PaymentAttemptId == attemptId);
        if (existing is not null)
        {
            ModelValidation.Require(existing.PaymentId == paymentId, nameof(paymentId), "Refund payment reference must remain unchanged.");
            return;
        }

        _refundRequests.Add(OrderRefundRequest.Create(this, paymentId, attemptId, reason, now));
    }
}
