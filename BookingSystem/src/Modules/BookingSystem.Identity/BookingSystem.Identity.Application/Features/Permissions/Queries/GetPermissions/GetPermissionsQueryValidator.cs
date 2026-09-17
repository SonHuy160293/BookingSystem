using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Permissions.Queries.GetPermissions;

public sealed class GetPermissionsQueryValidator : IValidator<GetPermissionsQuery>
{
    public Task ValidateAsync(GetPermissionsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scopeType = query.Request.ScopeType?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(scopeType) && scopeType is not ("PLATFORM" or "TENANT" or "BRANCH"))
        {
            throw new CqrsValidationException(["ScopeType must be PLATFORM, TENANT, or BRANCH."]);
        }

        return Task.CompletedTask;
    }
}
