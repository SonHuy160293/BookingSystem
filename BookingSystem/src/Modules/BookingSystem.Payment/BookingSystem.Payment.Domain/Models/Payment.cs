using BookingSystem.Payment.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Payment.Domain.Models;

public sealed class Payment : Entity<Guid>
{
    private readonly List<PaymentAttempt> _attempts = [];
    private readonly List<Refund> _refunds = [];
    private readonly List<PaymentReconciliationCase> _reconciliationCases = [];

    private Payment() { }

    public Guid OrderId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public PaymentStatus Status { get; private set; }
    public Guid? AcceptedAttemptId { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public int RefundCount { get; private set; }
    public int ReconciliationCaseCount { get; private set; }
    public long Revision { get; private set; } = 1;
    public PaymentAttempt? AcceptedAttempt { get; private set; }
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts.AsReadOnly();
    public IReadOnlyCollection<Refund> Refunds => _refunds.AsReadOnly();
    public IReadOnlyCollection<PaymentReconciliationCase> ReconciliationCases => _reconciliationCases.AsReadOnly();

    public static Payment Create(Guid orderId, Guid tenantId, decimal amount)
        => new()
        {
            Id = Guid.CreateVersion7(),
            OrderId = ModelValidation.Id(orderId, nameof(orderId)),
            TenantId = ModelValidation.Id(tenantId, nameof(tenantId)),
            Amount = ModelValidation.Money(amount, nameof(amount)),
            Status = PaymentStatus.Pending
        };

    public PaymentAttempt StartAttempt(string provider, string account, DateTimeOffset now)
    {
        EnsureComplete();
        ModelValidation.Require(Status == PaymentStatus.Pending && !_attempts.Any(a => a.IsActive)
            && !_reconciliationCases.Any(c => c.Status != ReconciliationStatus.Resolved),
            nameof(Status), "Pending payments require no active attempt or unresolved review.");
        int count = checked(AttemptCount + 1);
        long revision = checked(Revision + 1);
        PaymentAttempt attempt = PaymentAttempt.Create(this, provider, account, now);
        _attempts.Add(attempt);
        AttemptCount = count;
        Revision = revision;
        return attempt;
    }

    public void MarkAttemptPending(Guid attemptId, string providerPaymentId)
    {
        EnsureComplete();
        long revision = checked(Revision + 1);
        FindAttempt(attemptId).MarkPending(providerPaymentId);
        Revision = revision;
    }

    public void MarkAttemptUnknown(Guid attemptId, string failureCode, DateTimeOffset now)
    {
        EnsureComplete();
        PaymentAttempt attempt = FindAttempt(attemptId);
        if (!attempt.IsActive)
        {
            return;
        }

        string code = ModelValidation.Code(failureCode, nameof(failureCode));
        long revision = checked(Revision + 1);
        EnsureCase(attempt, ReconciliationReason.UnknownOutcome, now);
        attempt.MarkUnknown(code);
        Revision = revision;
    }

    public void RecordFailure(Guid attemptId, string failureCode, DateTimeOffset now, bool cancelled = false)
    {
        EnsureComplete();
        PaymentAttempt attempt = FindAttempt(attemptId);
        long revision = checked(Revision + 1);
        attempt.MarkFailed(failureCode, now, cancelled);
        ResolveUnknownCases(attempt.Id, now);
        Revision = revision;
    }

    public PaymentAcceptanceOutcome RecordSuccess(Guid attemptId, string providerPaymentId, string transactionId,
        decimal observedAmount, string observedCurrencyCode, DateTimeOffset now)
    {
        EnsureComplete();
        PaymentAttempt attempt = FindAttempt(attemptId);
        string reference = ModelValidation.Text(providerPaymentId, 200, nameof(providerPaymentId));
        string transaction = ModelValidation.Text(transactionId, 200, nameof(transactionId));
        ModelValidation.Require(now >= attempt.StartedAt, nameof(now), "Outcome time cannot precede the attempt.");
        long revision = checked(Revision + 1);

        if (!attempt.ReferencesMatch(reference, transaction))
        {
            EnsureCase(attempt, ReconciliationReason.LateOrDuplicateCharge, now);
            Revision = revision;
            return PaymentAcceptanceOutcome.NeedsReview;
        }

        if (observedAmount != Amount || observedCurrencyCode != CurrencyCode)
        {
            EnsureCase(attempt, ReconciliationReason.AmountOrCurrencyMismatch, now);
            attempt.MarkUnknown("Payment.AmountOrCurrencyMismatch");
            Revision = revision;
            return PaymentAcceptanceOutcome.NeedsReview;
        }

        if (attempt.Status == PaymentAttemptStatus.Succeeded)
        {
            return PaymentAcceptanceOutcome.AlreadyRecorded;
        }

        if (_reconciliationCases.Any(c => c.PaymentAttemptId == attempt.Id
            && c.Status != ReconciliationStatus.Resolved
            && c.Reason is ReconciliationReason.AmountOrCurrencyMismatch or ReconciliationReason.LateOrDuplicateCharge))
        {
            return PaymentAcceptanceOutcome.NeedsReview;
        }

        bool accepted = Status == PaymentStatus.Pending;
        if (!accepted)
        {
            EnsureCase(attempt, ReconciliationReason.LateOrDuplicateCharge, now);
        }

        attempt.MarkSucceeded(reference, transaction, now);
        ResolveUnknownCases(attempt.Id, now);
        if (accepted)
        {
            AcceptedAttemptId = attempt.Id;
            AcceptedAttempt = attempt;
            PaidAt = now.ToUniversalTime();
            Status = PaymentStatus.Paid;
        }

        Revision = revision;
        return accepted ? PaymentAcceptanceOutcome.Accepted : PaymentAcceptanceOutcome.RefundRequired;
    }

    public Refund RequestFullRefund(Guid attemptId, Guid? orderRefundRequestId, DateTimeOffset now)
    {
        EnsureComplete();
        PaymentAttempt attempt = FindAttempt(attemptId);
        var existing = _refunds.Find(refund => refund.PaymentAttemptId == attemptId);
        if (existing is not null)
        {
            if (orderRefundRequestId.HasValue)
            {
                existing.AttachOrderRequest(orderRefundRequestId.Value);
            }

            return existing;
        }

        int count = checked(RefundCount + 1);
        long revision = checked(Revision + 1);
        Refund refund = Refund.Create(this, attempt, orderRefundRequestId, now);
        _refunds.Add(refund);
        RefundCount = count;
        Revision = revision;
        return refund;
    }

    public PaymentReconciliationCase RequireRefundReview(Guid attemptId, DateTimeOffset now)
    {
        EnsureComplete();
        long revision = checked(Revision + 1);
        var result = EnsureCase(FindAttempt(attemptId), ReconciliationReason.RefundFailure, now);
        Revision = revision;
        return result;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        long revision = checked(Revision + 1);
        Status = PaymentStatus.Closed;
        ClosedAt = now.ToUniversalTime();
        Revision = revision;
    }

    private PaymentAttempt FindAttempt(Guid id)
        => _attempts.Find(attempt => attempt.Id == id)
            ?? throw new BookingSystem.SharedKernel.Exceptions.ValidationException(
                [new BookingSystem.SharedKernel.Exceptions.ValidationError(nameof(id), "Attempt does not belong to this payment.")]);

    private PaymentReconciliationCase EnsureCase(PaymentAttempt attempt, ReconciliationReason reason, DateTimeOffset now)
    {
        var existing = _reconciliationCases.Find(c => c.PaymentAttemptId == attempt.Id && c.Reason == reason);
        if (existing is not null)
        {
            if (existing.Status == ReconciliationStatus.Resolved)
            {
                existing.Reopen(now);
            }

            return existing;
        }

        int count = checked(ReconciliationCaseCount + 1);
        var created = PaymentReconciliationCase.Create(this, attempt, reason, now);
        _reconciliationCases.Add(created);
        ReconciliationCaseCount = count;
        return created;
    }

    private void ResolveUnknownCases(Guid attemptId, DateTimeOffset now)
    {
        foreach (var item in _reconciliationCases.Where(c => c.PaymentAttemptId == attemptId && c.Reason == ReconciliationReason.UnknownOutcome))
        {
            item.Resolve(now);
        }
    }

    private void EnsureComplete()
        => ModelValidation.Require(_attempts.Count == AttemptCount && _refunds.Count == RefundCount
            && _reconciliationCases.Count == ReconciliationCaseCount, nameof(Attempts), "Load the complete payment aggregate before mutation.");
}
