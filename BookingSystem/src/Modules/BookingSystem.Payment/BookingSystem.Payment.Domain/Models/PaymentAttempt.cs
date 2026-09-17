using BookingSystem.Payment.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Payment.Domain.Models;

public sealed class PaymentAttempt : Entity<Guid>
{
    private PaymentAttempt() { }

    public Guid PaymentId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ProviderAccountKey { get; private set; } = null!;
    public string ProviderIdempotencyKey { get; private set; } = null!;
    public string? ProviderPaymentId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public PaymentAttemptStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public Payment Payment { get; private set; } = null!;

    internal static PaymentAttempt Create(Payment payment, string provider, string account, DateTimeOffset now)
    {
        Guid id = Guid.CreateVersion7();
        return new PaymentAttempt
        {
            Id = id,
            PaymentId = payment.Id,
            Payment = payment,
            Provider = ModelValidation.Code(provider, nameof(provider), 32),
            ProviderAccountKey = ModelValidation.Text(account, 64, nameof(account)),
            ProviderIdempotencyKey = "pay-" + id.ToString("N"),
            Amount = payment.Amount,
            Status = PaymentAttemptStatus.Created,
            IsActive = true,
            StartedAt = now.ToUniversalTime()
        };
    }

    internal bool ReferencesMatch(string paymentId, string transactionId)
        => (ProviderPaymentId is null || ProviderPaymentId == paymentId)
            && (ProviderTransactionId is null || ProviderTransactionId == transactionId);

    internal void MarkPending(string providerPaymentId)
    {
        string reference = ModelValidation.Text(providerPaymentId, 200, nameof(providerPaymentId));
        ModelValidation.Require(ProviderPaymentId is null || ProviderPaymentId == reference, nameof(providerPaymentId), "Provider reference cannot change.");
        if (!IsActive)
        {
            return;
        }

        ProviderPaymentId = reference;
        Status = PaymentAttemptStatus.Pending;
        FailureCode = null;
    }

    internal void MarkUnknown(string failureCode)
    {
        if (!IsActive)
        {
            return;
        }

        FailureCode = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = PaymentAttemptStatus.Unknown;
    }

    internal void MarkSucceeded(string providerPaymentId, string transactionId, DateTimeOffset now)
    {
        string reference = ModelValidation.Text(providerPaymentId, 200, nameof(providerPaymentId));
        string transaction = ModelValidation.Text(transactionId, 200, nameof(transactionId));
        ModelValidation.Require(ReferencesMatch(reference, transaction), nameof(transactionId), "Provider references cannot change.");
        ModelValidation.Require(now >= StartedAt, nameof(now), "Outcome time cannot precede the attempt.");
        ProviderPaymentId = reference;
        ProviderTransactionId = transaction;
        Status = PaymentAttemptStatus.Succeeded;
        IsActive = false;
        CompletedAt = now.ToUniversalTime();
        FailureCode = null;
    }

    internal void MarkFailed(string failureCode, DateTimeOffset now, bool cancelled)
    {
        if (!IsActive)
        {
            return;
        }

        string code = ModelValidation.Code(failureCode, nameof(failureCode));
        ModelValidation.Require(now >= StartedAt, nameof(now), "Outcome time cannot precede the attempt.");
        Status = cancelled ? PaymentAttemptStatus.Cancelled : PaymentAttemptStatus.Failed;
        IsActive = false;
        FailureCode = code;
        CompletedAt = now.ToUniversalTime();
    }
}
