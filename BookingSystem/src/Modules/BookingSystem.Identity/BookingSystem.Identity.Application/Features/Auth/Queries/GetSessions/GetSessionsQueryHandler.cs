using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Identity.Application.Features.Auth.Queries.GetSessions;

public sealed class GetSessionsQueryHandler : IQueryHandler<GetSessionsQuery, IReadOnlyList<AuthSessionDto>>
{
    private readonly IAuthSessionRepository _sessions;
    private readonly ICurrentUserProvider _currentUser;
    private readonly TimeProvider _clock;
    public GetSessionsQueryHandler(IAuthSessionRepository sessions, ICurrentUserProvider currentUser, TimeProvider clock)
    {
        _sessions = sessions;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<AuthSessionDto>>> HandleAsync(GetSessionsQuery query, CancellationToken cancellationToken)
    {
        var userId = AuthRequestValidation.GetUserId(_currentUser);
        Guid? sessionId = Guid.TryParse(_currentUser.GetCurrentUser()?.SessionId, out var id) ? id : null;
        return Result.Success(await _sessions.ListActiveAsync(userId, sessionId, _clock.GetUtcNow().UtcDateTime, cancellationToken));
    }
}
