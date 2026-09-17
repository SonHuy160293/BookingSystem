namespace BookingSystem.Payment.Domain.Enums;

public enum ReconciliationReason : short
{
    UnknownOutcome = 0,
    AmountOrCurrencyMismatch = 1,
    LateOrDuplicateCharge = 2,
    RefundFailure = 3,
}
