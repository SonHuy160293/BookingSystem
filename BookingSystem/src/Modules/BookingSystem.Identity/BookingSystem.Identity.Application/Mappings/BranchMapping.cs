using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Mappings;

public static class BranchMapping
{
    public static BranchDto ToDto(Branch branch)
        => new(
            branch.Id,
            branch.Name,
            branch.Address,
            branch.CreatedAt);
}
