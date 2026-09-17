using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IAuthSessionRepository _sessions;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IRefreshTokenService _tokenService;
    private readonly TimeProvider _clock;
    private readonly ILogger<LogoutCommandHandler> _logger;
    public LogoutCommandHandler(
        IAuthSessionRepository sessions,
        IRefreshTokenRepository tokens,
        IRefreshTokenService tokenService,
        TimeProvider clock,
        ILogger<LogoutCommandHandler> logger)
    {
        _sessions = sessions;
        _tokens = tokens;
        _tokenService = tokenService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (!_tokenService.TryHash(command.Request.RefreshToken, out var hash))
        {
            return Result.Success();
        }

        var owner = await _tokens.FindOwnerAsync(hash, cancellationToken);
        if (owner is null)
        {
            return Result.Success();
        }

        await _sessions.LockUserAsync(owner.Value, cancellationToken);
        var token = await _tokens.FindByHashAsync(hash, cancellationToken);
        var session = token is null ? null : await _sessions.FindSessionAsync(token.SessionId, cancellationToken);
        if (session is not null && session.UserId == owner && string.Equals(session.ClientId, command.Request.ClientId, StringComparison.Ordinal) && session.RevokedAt is null)
        {
            session.Revoke(_clock.GetUtcNow().UtcDateTime, "Logout");
            _logger.LogInformation("Revoking authentication session {SessionId}: Logout", session.Id);
        }

        return Result.Success();
    }
}
