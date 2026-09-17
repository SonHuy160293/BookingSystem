using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Security;
using Microsoft.Extensions.Configuration;

namespace BookingSystem.Identity.Application.Features.Auth;

public sealed class AuthResponseFactory
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;
    private readonly IConfiguration _configuration;
    public AuthResponseFactory(IUserRepository users, IJwtTokenService jwt, IConfiguration configuration)
    {
        _users = users;
        _jwt = jwt;
        _configuration = configuration;
    }

    public async Task<AuthResponse> CreateAsync(
        User user,
        AuthSession session,
        string refreshToken,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var assignments = await _users.GetUserRoleAsync(user.Id, cancellationToken);
        var roles = assignments.Select(assignment => assignment.Role.Name).OfType<string>().Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var permissions = assignments.SelectMany(assignment => assignment.Role.RoleClaims).Where(claim => claim.ClaimType == CustomClaims.Permission && !string.IsNullOrWhiteSpace(claim.ClaimValue)).Select(claim => claim.ClaimValue!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        int minutes = _configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 5);
        if (minutes <= 0 || minutes > 5)
        {
            throw new InvalidOperationException("Access token lifetime must be between one and five minutes.");
        }

        var expiresAt = new DateTimeOffset(new DateTime(Math.Min(now.AddMinutes(minutes).Ticks, session.AbsoluteExpiresAt.Ticks), DateTimeKind.Utc));
        string accessToken = _jwt.CreateToken(user, assignments, session.Id, new DateTimeOffset(now), expiresAt);
        var account = new AccountUserDto(
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.JobTitle,
            user.IsEnabled,
            user.CreatedAt,
            roles,
            permissions);
        return new AuthResponse(accessToken, account, refreshToken, session.Id, expiresAt, new DateTimeOffset(session.IdleExpiresAt));
    }
}
