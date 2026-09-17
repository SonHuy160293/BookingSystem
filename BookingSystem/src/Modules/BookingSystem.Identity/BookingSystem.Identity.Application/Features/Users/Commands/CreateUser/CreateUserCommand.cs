using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(CreateUserRequest CreateUserRequest) : ICommand<CreateUserResponse>;
