namespace BookingSystem.Order.Domain.Enums;

public enum BookingConfirmationOutcome : short
{
    Confirmed = 0,
    AlreadyConfirmed = 1,
    RefundRequired = 2,
}
