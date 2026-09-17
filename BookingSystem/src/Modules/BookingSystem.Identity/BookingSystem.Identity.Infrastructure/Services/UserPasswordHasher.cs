using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Identity.Infrastructure.Services;

public sealed class UserPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
        => _passwordHasher.HashPassword(user, password);
}
