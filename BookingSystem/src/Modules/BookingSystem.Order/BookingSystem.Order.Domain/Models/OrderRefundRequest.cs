using BookingSystem.Order.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Order.Domain.Models;

public sealed class OrderRefundRequest : Entity<Guid>
{
    private OrderRefundRequest() { }

    public Guid OrderId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid PaymentAttemptId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public OrderRefundReason Reason { get; private set; }
    public OrderRefundStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public BookingOrder Order { get; private set; } = null!;

    internal static OrderRefundRequest Create(BookingOrder order, Guid paymentId, Guid attemptId,
        OrderRefundReason reason, DateTimeOffset now)
    {
        ModelValidation.Require(Enum.IsDefined(reason), nameof(reason), "Refund reason is invalid.");
        return new OrderRefundRequest
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            Order = order,
            PaymentId = ModelValidation.Id(paymentId, nameof(paymentId)),
            PaymentAttemptId = ModelValidation.Id(attemptId, nameof(attemptId)),
            Amount = ModelValidation.Money(order.TotalAmount, nameof(order.TotalAmount), positive: true),
            Reason = reason,
            Status = OrderRefundStatus.Requested,
            RequestedAt = now.ToUniversalTime()
        };
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == OrderRefundStatus.Completed)
        {
            return;
        }

        ModelValidation.Require(now >= RequestedAt, nameof(now), "Completion cannot precede request.");
        Status = OrderRefundStatus.Completed;
        CompletedAt = now.ToUniversalTime();
        FailureCode = null;
    }

    public void Fail(string failureCode)
    {
        ModelValidation.Require(Status != OrderRefundStatus.Completed, nameof(Status), "Completed refunds cannot fail.");
        FailureCode = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = OrderRefundStatus.Failed;
    }

    public void Retry()
    {
        ModelValidation.Require(Status == OrderRefundStatus.Failed, nameof(Status), "Only failed requests can be retried.");
        Status = OrderRefundStatus.Requested;
        FailureCode = null;
    }
}
