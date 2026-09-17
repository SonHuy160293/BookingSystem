using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Abstractions.Services;

public interface IJwtTokenService
{
    string CreateToken(User user, IReadOnlyList<UserRole> userRoles, Guid sessionId,
        DateTimeOffset issuedAt, DateTimeOffset expiresAt);
}
