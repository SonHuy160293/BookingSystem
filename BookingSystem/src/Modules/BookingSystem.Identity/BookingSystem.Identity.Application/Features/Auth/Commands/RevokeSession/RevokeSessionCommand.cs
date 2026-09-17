using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand, IAuthCommand;
