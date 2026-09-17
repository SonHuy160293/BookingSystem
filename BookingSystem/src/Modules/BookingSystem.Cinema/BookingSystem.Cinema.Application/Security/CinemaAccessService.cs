using BookingSystem.Cinema.Application.Abstractions.Integrations;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Security;

public sealed class CinemaAccessService : ICinemaAccessService
{
    private readonly IIdentityCinemaClient _identity;
    private readonly ICurrentUserProvider _currentUser;
    private readonly Dictionary<string, CinemaAccessDto> _scopes = new(StringComparer.Ordinal);

    public CinemaAccessService(IIdentityCinemaClient identity, ICurrentUserProvider currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<CinemaAccessDto> GetCinemaScopeAsync(ActionType action, CancellationToken cancellationToken)
    {
        var scope = await GetAccessAsync(Functions.Cinemas, action, cancellationToken);
        if (!scope.HasPlatformAccess && scope.TenantIds.Length == 0 && scope.Branches.Length == 0)
        {
            throw new ForbiddenException();
        }

        return scope;
    }

    public async Task RequireFilmAccessAsync(ActionType action, CancellationToken cancellationToken)
    {
        var scope = await GetAccessAsync(Functions.Films, action, cancellationToken);
        if (!scope.HasPlatformAccess)
        {
            throw new ForbiddenException();
        }
    }

    public async Task<BranchAccessDto> RequireBranchAsync(Guid branchId, CinemaAccessDto scope, CancellationToken cancellationToken)
    {
        var branch = await _identity.GetBranchAsync(branchId, cancellationToken);
        if (!scope.HasPlatformAccess && !scope.TenantIds.Contains(branch.TenantId)
            && !scope.Branches.Any(allowed => allowed.Id == branch.Id && allowed.TenantId == branch.TenantId))
        {
            throw new NotFoundException("Branch.NotFound", "Branch was not found.");
        }

        return branch;
    }

    private async Task<CinemaAccessDto> GetAccessAsync(string function, ActionType action, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.GetCurrentUser()?.Id, out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedException();
        }

        string permission = PermissionCatalog.ToValue(function, action);
        if (!_scopes.TryGetValue(permission, out var scope))
        {
            scope = await _identity.GetAccessAsync(userId, permission, cancellationToken);
            _scopes.Add(permission, scope);
        }

        return scope;
    }
}
