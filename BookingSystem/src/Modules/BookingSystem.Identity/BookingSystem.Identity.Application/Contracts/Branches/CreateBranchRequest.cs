namespace BookingSystem.Identity.Application.Contracts.Branches;

public sealed record CreateBranchRequest(string Name, string? Address = null);
