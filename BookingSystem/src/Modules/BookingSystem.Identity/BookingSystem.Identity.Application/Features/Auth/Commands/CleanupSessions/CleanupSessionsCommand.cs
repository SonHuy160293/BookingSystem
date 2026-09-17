using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.CleanupSessions;

public sealed record CleanupSessionsCommand(Guid UserId) : ICommand<int>, IAuthCommand;
