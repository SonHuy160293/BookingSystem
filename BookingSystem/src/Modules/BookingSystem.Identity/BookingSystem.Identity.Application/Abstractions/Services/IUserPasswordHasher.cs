using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Abstractions.Services;

public interface IUserPasswordHasher
{
    string HashPassword(User user, string password);
}
