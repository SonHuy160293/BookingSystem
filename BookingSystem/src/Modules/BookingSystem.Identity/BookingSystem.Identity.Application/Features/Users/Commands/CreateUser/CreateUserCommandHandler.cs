using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Application.Mappings;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, AccountUserDto>
{
    private readonly IUserPasswordHasher _passwordHasher;
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository, IUserPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AccountUserDto>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        if (await _repository.NormalizedUserNameExistsAsync(normalizedUserName, cancellationToken))
        {
            throw new ConflictException("User.UserNameAlreadyExists", $"User name '{userName}' already exists.");
        }

        if (await _repository.NormalizedEmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("User.EmailAlreadyExists", $"Email '{email}' already exists.");
        }

        var user = User.Create(
            request.IsEnabled,
            request.FullName,
            userName,
            email,
            request.JobTitle,
            null);
        var passwordHash = _passwordHasher.HashPassword(user, request.Password);

        user.SetIdentityCredentials(
            normalizedUserName,
            normalizedEmail,
            passwordHash,
            Guid.CreateVersion7().ToString(),
            Guid.CreateVersion7().ToString());

        await _repository.AddAsync(user, cancellationToken);

        return Result.Success(UserMapping.ToDto(user));
    }
}
