using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.LogoutAll;

public sealed record LogoutAllCommand : ICommand, IAuthCommand;
