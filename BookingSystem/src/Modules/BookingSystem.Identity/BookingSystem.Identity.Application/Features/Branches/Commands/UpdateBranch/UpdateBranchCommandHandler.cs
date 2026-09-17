using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Application.Mappings;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Branches.Commands.UpdateBranch;

public sealed class UpdateBranchCommandHandler : ICommandHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _repository;

    public UpdateBranchCommandHandler(IBranchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BranchDto>> HandleAsync(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await _repository.GetByIdAsync(command.Id, cancellationToken);
        branch.ChangeDetails(command.Request.Name, command.Request.Address);
        await _repository.UpdateAsync(branch, cancellationToken);

        return Result.Success(BranchMapping.ToDto(branch));
    }
}
