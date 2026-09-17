namespace BookingSystem.Order.Domain.Enums;

public enum SeatAllocationStatus : short
{
    Available = 0,
    Held = 1,
    Sold = 2,
    Blocked = 3,
}
