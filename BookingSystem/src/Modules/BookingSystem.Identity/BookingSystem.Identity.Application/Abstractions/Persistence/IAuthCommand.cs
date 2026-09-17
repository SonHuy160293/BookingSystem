namespace BookingSystem.Identity.Application.Abstractions.Persistence;

// Authentication mutations cannot be replayed automatically after an uncertain commit.
public interface IAuthCommand
{
}
