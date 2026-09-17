namespace BookingSystem.Identity.Application.Contracts.Branches;

public sealed record UpdateBranchRequest(string Name, string? Address = null);
