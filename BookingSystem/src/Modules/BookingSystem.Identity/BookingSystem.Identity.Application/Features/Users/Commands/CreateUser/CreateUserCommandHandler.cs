using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreateUserResponse>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.CreateUserRequest;

        var existingUser = await _repository.EmailExistsAsync(request.Email, cancellationToken);

        throw new NotImplementedException();

        //if (existingUser)
        //{
        //    throw new CqrsValidationException([$"User with email '{request.Email}' already exists."]);
        //}

        //var user = User.Create(request.Name, request.Email, request.Password);
        //await _repository.AddAsync(user, cancellationToken);

        //return Result.Success(new CreateUserResponse(user.Id.ToString(), user.Name, user.Email));
    }
}
