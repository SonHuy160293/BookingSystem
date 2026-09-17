using BookingSystem.Payment.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Payment.Domain.Models;

public sealed class PaymentReconciliationCase : Entity<Guid>
{
    private PaymentReconciliationCase() { }

    public Guid PaymentId { get; private set; }
    public Guid PaymentAttemptId { get; private set; }
    public ReconciliationReason Reason { get; private set; }
    public ReconciliationStatus Status { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public PaymentAttempt PaymentAttempt { get; private set; } = null!;

    internal static PaymentReconciliationCase Create(Payment payment, PaymentAttempt attempt,
        ReconciliationReason reason, DateTimeOffset now)
    {
        ModelValidation.Require(Enum.IsDefined(reason) && attempt.PaymentId == payment.Id, nameof(reason), "Case reason or attempt is invalid.");
        return new PaymentReconciliationCase
        {
            Id = Guid.CreateVersion7(),
            PaymentId = payment.Id,
            Payment = payment,
            PaymentAttemptId = attempt.Id,
            PaymentAttempt = attempt,
            Reason = reason,
            Status = ReconciliationStatus.Open,
            NextAttemptAt = now.ToUniversalTime()
        };
    }

    public void Claim(Guid token, DateTimeOffset now, DateTimeOffset until)
    {
        ModelValidation.Lease(token, now, until);
        ModelValidation.Require(Status == ReconciliationStatus.Open && NextAttemptAt <= now
            && (!LeaseUntil.HasValue || LeaseUntil <= now), nameof(Status), "Case is not claimable.");
        int attempts = checked(AttemptCount + 1);
        LeaseToken = token;
        LeaseUntil = until.ToUniversalTime();
        AttemptCount = attempts;
    }

    public void Retry(Guid token, DateTimeOffset now, DateTimeOffset nextAttemptAt, string failureCode, bool manualReview)
    {
        ModelValidation.OwnLease(token, LeaseToken, LeaseUntil, now);
        ModelValidation.Require(nextAttemptAt >= now, nameof(nextAttemptAt), "Retry time is invalid.");
        string code = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = manualReview ? ReconciliationStatus.ManualReview : ReconciliationStatus.Open;
        FailureCode = code;
        NextAttemptAt = nextAttemptAt.ToUniversalTime();
        LeaseToken = null;
        LeaseUntil = null;
    }

    public void CompleteClaim(Guid token, DateTimeOffset now)
    {
        ModelValidation.OwnLease(token, LeaseToken, LeaseUntil, now);
        Resolve(now);
    }

    // A verified callback or an explicit review decision may supersede a worker.
    public void Resolve(DateTimeOffset now)
    {
        if (Status == ReconciliationStatus.Resolved)
        {
            return;
        }

        Status = ReconciliationStatus.Resolved;
        ResolvedAt = now.ToUniversalTime();
        FailureCode = null;
        LeaseToken = null;
        LeaseUntil = null;
    }

    public void Reopen(DateTimeOffset now)
    {
        ModelValidation.Require(Status is ReconciliationStatus.Resolved or ReconciliationStatus.ManualReview,
            nameof(Status), "Only settled or reviewed cases can be reopened.");
        Status = ReconciliationStatus.Open;
        NextAttemptAt = now.ToUniversalTime();
        ResolvedAt = null;
    }
}
