using BookingSystem.Payment.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Payment.Domain.Models;

public sealed class Refund : Entity<Guid>
{
    private Refund() { }

    public Guid PaymentId { get; private set; }
    public Guid PaymentAttemptId { get; private set; }
    public Guid? OrderRefundRequestId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ProviderAccountKey { get; private set; } = null!;
    public string ProviderIdempotencyKey { get; private set; } = null!;
    public string? ProviderRefundId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public RefundStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public PaymentAttempt PaymentAttempt { get; private set; } = null!;

    internal static Refund Create(Payment payment, PaymentAttempt attempt, Guid? requestId, DateTimeOffset now)
    {
        ModelValidation.Require(attempt.PaymentId == payment.Id && attempt.Status == PaymentAttemptStatus.Succeeded,
            nameof(attempt), "A successful attempt from this payment is required.");
        if (requestId.HasValue)
        {
            ModelValidation.Id(requestId.Value, nameof(requestId));
        }

        Guid id = Guid.CreateVersion7();
        return new Refund
        {
            Id = id,
            PaymentId = payment.Id,
            Payment = payment,
            PaymentAttemptId = attempt.Id,
            PaymentAttempt = attempt,
            OrderRefundRequestId = requestId,
            Provider = attempt.Provider,
            ProviderAccountKey = attempt.ProviderAccountKey,
            ProviderIdempotencyKey = "refund-" + id.ToString("N"),
            Amount = attempt.Amount,
            Status = RefundStatus.Requested,
            RequestedAt = now.ToUniversalTime(),
            NextAttemptAt = now.ToUniversalTime()
        };
    }

    internal void AttachOrderRequest(Guid requestId)
    {
        ModelValidation.Id(requestId, nameof(requestId));
        ModelValidation.Require(!OrderRefundRequestId.HasValue || OrderRefundRequestId == requestId,
            nameof(requestId), "Refund request reference cannot change.");
        OrderRefundRequestId = requestId;
    }

    public void Claim(Guid token, DateTimeOffset now, DateTimeOffset until)
    {
        ModelValidation.Lease(token, now, until);
        ModelValidation.Require(Status != RefundStatus.Succeeded && NextAttemptAt <= now && (!LeaseUntil.HasValue || LeaseUntil <= now),
            nameof(Status), "Refund is not claimable.");
        int attempts = checked(AttemptCount + 1);
        Status = RefundStatus.Pending;
        LeaseToken = token;
        LeaseUntil = until.ToUniversalTime();
        AttemptCount = attempts;
    }

    public void ScheduleRetry(Guid token, DateTimeOffset now, DateTimeOffset nextAttemptAt, string failureCode, bool unknown)
    {
        ModelValidation.OwnLease(token, LeaseToken, LeaseUntil, now);
        ModelValidation.Require(nextAttemptAt >= now && Status != RefundStatus.Succeeded, nameof(nextAttemptAt), "Retry time is invalid.");
        string code = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = unknown ? RefundStatus.Unknown : RefundStatus.Failed;
        FailureCode = code;
        NextAttemptAt = nextAttemptAt.ToUniversalTime();
        LeaseToken = null;
        LeaseUntil = null;
    }

    // Used by a verified provider event; a worker must call CompleteClaim instead.
    public void RecordProviderSuccess(string providerRefundId, DateTimeOffset now)
    {
        string reference = ModelValidation.Text(providerRefundId, 200, nameof(providerRefundId));
        ModelValidation.Require(ProviderRefundId is null || ProviderRefundId == reference, nameof(providerRefundId), "Provider refund reference cannot change.");
        if (Status == RefundStatus.Succeeded)
        {
            return;
        }

        ModelValidation.Require(now >= RequestedAt, nameof(now), "Completion cannot precede request.");
        ProviderRefundId = reference;
        Status = RefundStatus.Succeeded;
        CompletedAt = now.ToUniversalTime();
        FailureCode = null;
        LeaseToken = null;
        LeaseUntil = null;
    }

    public void CompleteClaim(Guid token, string providerRefundId, DateTimeOffset now)
    {
        ModelValidation.OwnLease(token, LeaseToken, LeaseUntil, now);
        RecordProviderSuccess(providerRefundId, now);
    }
}
