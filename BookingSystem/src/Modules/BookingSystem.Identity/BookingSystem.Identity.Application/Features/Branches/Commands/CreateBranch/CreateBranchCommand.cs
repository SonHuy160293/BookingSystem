using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Branches.Commands.CreateBranch;

public sealed record CreateBranchCommand(CreateBranchRequest Request) : ICommand<BranchDto>;
