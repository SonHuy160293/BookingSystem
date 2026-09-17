using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Microsoft.Extensions.Options;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.CleanupSessions;

public sealed class CleanupSessionsCommandHandler : ICommandHandler<CleanupSessionsCommand, int>
{
    private readonly IAuthSessionRepository _sessions;
    private readonly AuthSessionOptions _options;
    private readonly TimeProvider _clock;
    public CleanupSessionsCommandHandler(IAuthSessionRepository sessions, IOptions<AuthSessionOptions> options, TimeProvider clock)
    {
        _sessions = sessions;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<Result<int>> HandleAsync(CleanupSessionsCommand command, CancellationToken cancellationToken)
    {
        await _sessions.LockUserAsync(command.UserId, cancellationToken);
        var cutoff = _clock.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);
        return Result.Success(await _sessions.DeleteExpiredFamiliesAsync(command.UserId, cutoff, _options.CleanupBatchSize, cancellationToken));
    }
}
