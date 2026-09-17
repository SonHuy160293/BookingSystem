namespace BookingSystem.Order.Domain.Enums;

public enum BookingOrderStatus : short
{
    AwaitingPayment = 0,
    Confirmed = 1,
    Expired = 2,
    Cancelled = 3,
}
