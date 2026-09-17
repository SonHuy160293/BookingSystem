namespace BookingSystem.Payment.Domain.Enums;

public enum ProviderEventStatus : short
{
    Received = 0,
    Processed = 1,
    NeedsReview = 2,
    Rejected = 3,
}
