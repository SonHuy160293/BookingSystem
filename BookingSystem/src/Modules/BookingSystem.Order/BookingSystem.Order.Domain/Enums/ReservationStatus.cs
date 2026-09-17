namespace BookingSystem.Order.Domain.Enums;

public enum ReservationStatus : short
{
    Active = 0,
    Converted = 1,
    Confirmed = 2,
    Expired = 3,
    Released = 4,
}
