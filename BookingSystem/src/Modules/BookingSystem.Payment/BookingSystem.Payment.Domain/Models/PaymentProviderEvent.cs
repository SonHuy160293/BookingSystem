using BookingSystem.Payment.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Payment.Domain.Models;

public sealed class PaymentProviderEvent : Entity<Guid>
{
    private PaymentProviderEvent() { }

    public string Provider { get; private set; } = null!;
    public string ProviderAccountKey { get; private set; } = null!;
    public string EventId { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public Guid? AttemptId { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public decimal? ObservedAmount { get; private set; }
    public string? ObservedCurrencyCode { get; private set; }
    public string PayloadHash { get; private set; } = null!;
    public DateTimeOffset VerifiedAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public ProviderEventStatus Status { get; private set; }
    public string? FailureCode { get; private set; }
    public PaymentAttempt? Attempt { get; private set; }

    // The caller must authenticate the provider callback before creating this record.
    public static PaymentProviderEvent CreateVerified(string provider, string account, string eventId, string eventType,
        string? providerPaymentId, string? transactionId, decimal? amount, string? currencyCode,
        string payloadHash, DateTimeOffset receivedAt, DateTimeOffset verifiedAt)
    {
        ModelValidation.Require(verifiedAt >= receivedAt, nameof(verifiedAt), "Verification cannot precede receipt.");
        ModelValidation.Require(amount.HasValue == (currencyCode is not null), nameof(amount), "Amount and currency must be supplied together.");
        if (amount.HasValue)
        {
            ModelValidation.Require(amount.Value >= 0 && amount.Value <= 999_999_999_999_999_999.999999m
                && decimal.Round(amount.Value, 6) == amount.Value, nameof(amount), "Observed amount exceeds the supported precision.");
            ModelValidation.Require(currencyCode is { Length: 3 } && currencyCode.All(char.IsAsciiLetter),
                nameof(currencyCode), "Observed currency must contain three ASCII letters.");
        }

        return new PaymentProviderEvent
        {
            Id = Guid.CreateVersion7(),
            Provider = ModelValidation.Code(provider, nameof(provider), 32),
            ProviderAccountKey = ModelValidation.Text(account, 64, nameof(account)),
            EventId = ModelValidation.Text(eventId, 200, nameof(eventId)),
            EventType = ModelValidation.Text(eventType, 200, nameof(eventType)),
            ProviderPaymentId = providerPaymentId is null ? null : ModelValidation.Text(providerPaymentId, 200, nameof(providerPaymentId)),
            ProviderTransactionId = transactionId is null ? null : ModelValidation.Text(transactionId, 200, nameof(transactionId)),
            ObservedAmount = amount,
            ObservedCurrencyCode = currencyCode?.ToUpperInvariant(),
            PayloadHash = ModelValidation.Hash(payloadHash, nameof(payloadHash)),
            ReceivedAt = receivedAt.ToUniversalTime(),
            VerifiedAt = verifiedAt.ToUniversalTime(),
            Status = ProviderEventStatus.Received
        };
    }

    public void EnsurePayloadMatches(string payloadHash)
    {
        ModelValidation.Require(PayloadHash == ModelValidation.Hash(payloadHash, nameof(payloadHash)),
            nameof(payloadHash), "Provider event identifier was reused with another payload.");
    }

    public void AttachAttempt(PaymentAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ModelValidation.Require(Provider == attempt.Provider && ProviderAccountKey == attempt.ProviderAccountKey
            && (!AttemptId.HasValue || AttemptId == attempt.Id)
            && (attempt.ProviderPaymentId is null || ProviderPaymentId is null || attempt.ProviderPaymentId == ProviderPaymentId),
            nameof(attempt), "Provider event and attempt do not match.");
        AttemptId = attempt.Id;
        Attempt = attempt;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        if (Status == ProviderEventStatus.Processed)
        {
            return;
        }

        ModelValidation.Require(Status != ProviderEventStatus.Rejected && now >= VerifiedAt, nameof(Status), "Event cannot be processed.");
        ModelValidation.Require(AttemptId.HasValue, nameof(AttemptId), "A mapped attempt is required for processing.");
        Status = ProviderEventStatus.Processed;
        ProcessedAt = now.ToUniversalTime();
        FailureCode = null;
    }

    public void RequireReview(string failureCode)
    {
        ModelValidation.Require(Status is ProviderEventStatus.Received or ProviderEventStatus.NeedsReview, nameof(Status), "Event outcome is already final.");
        FailureCode = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = ProviderEventStatus.NeedsReview;
    }

    public void Reject(string failureCode)
    {
        ModelValidation.Require(Status != ProviderEventStatus.Processed, nameof(Status), "A processed event cannot be rejected.");
        FailureCode = ModelValidation.Code(failureCode, nameof(failureCode));
        Status = ProviderEventStatus.Rejected;
    }
}
