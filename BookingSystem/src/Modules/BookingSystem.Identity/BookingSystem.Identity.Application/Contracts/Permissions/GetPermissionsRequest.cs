using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Contracts.Permissions;

public sealed record GetPermissionsRequest : PagedRequest
{
    public string? ScopeType { get; init; }
}
