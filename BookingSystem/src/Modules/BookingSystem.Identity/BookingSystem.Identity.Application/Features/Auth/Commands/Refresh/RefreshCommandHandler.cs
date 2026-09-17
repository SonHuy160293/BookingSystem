using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler : ICommandHandler<RefreshCommand, AuthResponse>
{
    private static readonly Error InvalidToken = new("Auth.InvalidRefreshToken", "Authentication failed. Please sign in again.");
    private readonly IAuthSessionRepository _sessions;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IRefreshTokenService _tokenService;
    private readonly AuthResponseFactory _responses;
    private readonly AuthSessionOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<RefreshCommandHandler> _logger;

    public RefreshCommandHandler(
        IAuthSessionRepository sessions,
        IRefreshTokenRepository tokens,
        IRefreshTokenService tokenService,
        AuthResponseFactory responses,
        IOptions<AuthSessionOptions> options,
        TimeProvider clock,
        ILogger<RefreshCommandHandler> logger)
    {
        _sessions = sessions;
        _tokens = tokens;
        _tokenService = tokenService;
        _responses = responses;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> HandleAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!_options.IsAllowedClient(request.ClientId) || !_tokenService.TryHash(request.RefreshToken, out var hash))
        {
            return Result.Failure<AuthResponse>(InvalidToken);
        }

        var owner = await _tokens.FindOwnerAsync(hash, cancellationToken);
        if (owner is null)
        {
            return Result.Failure<AuthResponse>(InvalidToken);
        }

        var user = await _sessions.LockUserAsync(owner.Value, cancellationToken);
        var token = await _tokens.FindByHashAsync(hash, cancellationToken);
        var session = token is null ? null : await _sessions.FindSessionAsync(token.SessionId, cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        if (session is null || session.UserId != owner
            || !string.Equals(session.ClientId, request.ClientId, StringComparison.Ordinal) || !session.IsActive(now))
        {
            return Result.Failure<AuthResponse>(InvalidToken);
        }

        string? reason = token!.ConsumedAt is not null ? "Replay"
            : user is null || !AuthRequestValidation.CanAuthenticate(user, now) ? "AccountIneligible"
            : !string.Equals(session.SecurityStampAtLogin, user.SecurityStamp, StringComparison.Ordinal) ? "SecurityStampChanged"
            : session.CurrentRefreshTokenId != token.Id ? "TokenChainInvalid" : null;
        if (reason is not null)
        {
            session.Revoke(now, reason);
            _logger.LogWarning("Revoking authentication session {SessionId}: {Reason}", session.Id, reason);

            // A failure Result still commits: throwing here would roll back replay revocation.
            return Result.Failure<AuthResponse>(InvalidToken);
        }

        if (now >= token.ExpiresAt)
        {
            return Result.Failure<AuthResponse>(InvalidToken);
        }

        string rawToken = _tokenService.Generate();
        _tokenService.TryHash(rawToken, out var newHash);
        var newExpiry = new DateTime(Math.Min(now.AddDays(_options.IdleExpirationDays).Ticks, session.AbsoluteExpiresAt.Ticks), DateTimeKind.Utc);
        var successor = RefreshToken.Create(session.Id, newHash, now, newExpiry);
        token.Consume(now, successor.Id);
        session.Rotate(token.Id, successor.Id, now, TimeSpan.FromDays(_options.IdleExpirationDays));
        await _tokens.AddAsync(successor, cancellationToken);
        var response = await _responses.CreateAsync(user!, session, rawToken, now, cancellationToken);
        _logger.LogInformation("Rotating authentication session {SessionId}", session.Id);
        return Result.Success(response);
    }
}
