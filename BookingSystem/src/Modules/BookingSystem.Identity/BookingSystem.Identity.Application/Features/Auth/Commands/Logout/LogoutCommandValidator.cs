using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandValidator : IValidator<LogoutCommand>
{
    private readonly IRefreshTokenService _tokens;
    public LogoutCommandValidator(IRefreshTokenService tokens) => _tokens = tokens;
    public Task ValidateAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AuthRequestValidation.ValidateTokenRequest(command.Request, _tokens);
        return Task.CompletedTask;
    }
}
