namespace BookingSystem.Payment.Domain.Enums;

public enum PaymentAttemptStatus : short
{
    Created = 0,
    Pending = 1,
    Unknown = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5,
}
