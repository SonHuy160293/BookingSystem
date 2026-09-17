namespace BookingSystem.Order.Domain.Enums;

public enum OrderRefundReason : short
{
    LatePayment = 0,
    ShowtimeCancellation = 1,
    DuplicateCharge = 2,
}
