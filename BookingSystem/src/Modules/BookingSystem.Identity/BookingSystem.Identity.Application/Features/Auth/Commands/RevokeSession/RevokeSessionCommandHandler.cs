using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.RevokeSession;

public sealed class RevokeSessionCommandHandler : ICommandHandler<RevokeSessionCommand>
{
    private readonly IAuthSessionRepository _sessions;
    private readonly ICurrentUserProvider _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<RevokeSessionCommandHandler> _logger;
    public RevokeSessionCommandHandler(
        IAuthSessionRepository sessions,
        ICurrentUserProvider currentUser,
        TimeProvider clock,
        ILogger<RevokeSessionCommandHandler> logger)
    {
        _sessions = sessions;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var userId = AuthRequestValidation.GetUserId(_currentUser);
        await _sessions.LockUserAsync(userId, cancellationToken);
        var session = await _sessions.FindSessionAsync(command.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            throw new NotFoundException("Auth.SessionNotFound", "The session was not found.");
        }

        if (session.RevokedAt is null)
        {
            session.Revoke(_clock.GetUtcNow().UtcDateTime, "DeviceRevoked");
            _logger.LogInformation("Revoking authentication session {SessionId}: DeviceRevoked", session.Id);
        }

        return Result.Success();
    }
}
