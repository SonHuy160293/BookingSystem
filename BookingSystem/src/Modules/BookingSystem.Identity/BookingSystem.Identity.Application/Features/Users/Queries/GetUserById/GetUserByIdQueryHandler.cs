using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, AccountUserDto>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AccountUserDto>> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(query.Id, cancellationToken);
        return Result.Success(user);
    }
}
