using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandValidator : IValidator<RefreshCommand>
{
    private readonly IRefreshTokenService _tokens;
    public RefreshCommandValidator(IRefreshTokenService tokens) => _tokens = tokens;
    public Task ValidateAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AuthRequestValidation.ValidateTokenRequest(command.Request, _tokens);
        return Task.CompletedTask;
    }
}
