using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(LoginRequest Request) : ICommand<AuthResponse>, IAuthCommand;
