using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.LogoutAll;

public sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly IAuthSessionRepository _sessions;
    private readonly ICurrentUserProvider _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<LogoutAllCommandHandler> _logger;
    public LogoutAllCommandHandler(
        IAuthSessionRepository sessions,
        ICurrentUserProvider currentUser,
        TimeProvider clock,
        ILogger<LogoutAllCommandHandler> logger)
    {
        _sessions = sessions;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        var userId = AuthRequestValidation.GetUserId(_currentUser);
        await _sessions.LockUserAsync(userId, cancellationToken);
        var sessions = await _sessions.GetUnrevokedSessionsAsync(userId, cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var session in sessions)
        {
            session.Revoke(now, "LogoutAll");
            _logger.LogInformation("Revoking authentication session {SessionId}: LogoutAll", session.Id);
        }

        return Result.Success();
    }
}
