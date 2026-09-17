namespace BookingSystem.Payment.Domain.Enums;

public enum PaymentAcceptanceOutcome : short
{
    Accepted = 0,
    AlreadyRecorded = 1,
    RefundRequired = 2,
    NeedsReview = 3,
}
