using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Branches.Commands.UpdateBranch;

public sealed record UpdateBranchCommand(Guid Id, UpdateBranchRequest Request) : ICommand<BranchDto>;
