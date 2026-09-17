using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class Ticket : Entity<Guid>
{
    private Ticket() { }

    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ShowtimeId { get; private set; }
    public Guid ShowtimeSeatId { get; private set; }
    public string TicketCode { get; private set; } = null!;
    public string QrTokenHash { get; private set; } = null!;
    public TicketStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public string? VoidReasonCode { get; private set; }
    public BookingOrder Order { get; private set; } = null!;
    public OrderItem OrderItem { get; private set; } = null!;

    internal static Ticket Create(OrderItem item, TicketCredentials credentials, DateTimeOffset now)
    {
        ModelValidation.Require(item.Id == credentials.OrderItemId, nameof(credentials), "Ticket credentials must match the order item.");
        return new Ticket
        {
            Id = Guid.CreateVersion7(),
            OrderId = item.OrderId,
            Order = item.Order,
            OrderItemId = item.Id,
            OrderItem = item,
            ShowtimeId = item.ShowtimeId,
            ShowtimeSeatId = item.ShowtimeSeatId,
            TicketCode = ModelValidation.Code(credentials.TicketCode, nameof(credentials.TicketCode)),
            QrTokenHash = ModelValidation.Hash(credentials.QrTokenHash, nameof(credentials.QrTokenHash)),
            IssuedAt = now.ToUniversalTime(),
            Status = TicketStatus.Valid
        };
    }

    public void Redeem(DateTimeOffset now)
    {
        ModelValidation.Require(Status == TicketStatus.Valid && now >= IssuedAt, nameof(Status), "Ticket is not redeemable.");
        Status = TicketStatus.Used;
        UsedAt = now.ToUniversalTime();
    }

    internal void Void(string reasonCode, DateTimeOffset now)
    {
        if (Status == TicketStatus.Voided)
        {
            return;
        }

        string reason = ModelValidation.Code(reasonCode, nameof(reasonCode));
        ModelValidation.Require(now >= IssuedAt && (!UsedAt.HasValue || now >= UsedAt.Value), nameof(now), "Void time cannot precede ticket use or issuance.");
        Status = TicketStatus.Voided;
        VoidReasonCode = reason;
        VoidedAt = now.ToUniversalTime();
    }
}
