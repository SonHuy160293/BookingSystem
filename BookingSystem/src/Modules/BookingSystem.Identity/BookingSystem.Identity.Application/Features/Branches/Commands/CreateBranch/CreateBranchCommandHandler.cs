using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Application.Mappings;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Branches.Commands.CreateBranch;

public sealed class CreateBranchCommandHandler : ICommandHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _repository;

    public CreateBranchCommandHandler(IBranchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BranchDto>> HandleAsync(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = Branch.Create(command.Request.TenantId, command.Request.Name, command.Request.Address);
        await _repository.AddAsync(branch, cancellationToken);

        return Result.Success(BranchMapping.ToDto(branch));
    }
}
