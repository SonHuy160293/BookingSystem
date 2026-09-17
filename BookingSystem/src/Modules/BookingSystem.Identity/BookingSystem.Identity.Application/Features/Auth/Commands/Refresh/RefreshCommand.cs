using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshCommand(RefreshTokenRequest Request) : ICommand<AuthResponse>, IAuthCommand;
