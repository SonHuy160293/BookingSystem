namespace BookingSystem.Payment.Domain.Enums;

public enum RefundStatus : short
{
    Requested = 0,
    Pending = 1,
    Unknown = 2,
    Succeeded = 3,
    Failed = 4,
}
