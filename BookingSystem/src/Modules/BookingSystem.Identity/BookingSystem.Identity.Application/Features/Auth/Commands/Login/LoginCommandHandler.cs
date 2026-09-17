using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid email or password.");
    private readonly IAuthSessionRepository _sessions;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IUserPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _tokenService;
    private readonly AuthResponseFactory _responses;
    private readonly AuthSessionOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthSessionRepository sessions,
        IRefreshTokenRepository tokens,
        IUserPasswordHasher passwordHasher,
        IRefreshTokenService tokenService,
        AuthResponseFactory responses,
        IOptions<AuthSessionOptions> options,
        TimeProvider clock,
        ILogger<LoginCommandHandler> logger)
    {
        _sessions = sessions;
        _tokens = tokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _responses = responses;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        string clientId = request.ClientId ?? _options.DefaultClientId;
        var userId = await _sessions.FindUserIdByEmailAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
        var user = userId is null ? null : await _sessions.LockUserAsync(userId.Value, cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        if (user is null || !_options.IsAllowedClient(clientId) || !AuthRequestValidation.CanAuthenticate(user, now)
            || !string.Equals(user.NormalizedEmail, request.Email.Trim().ToUpperInvariant(), StringComparison.Ordinal)
            || string.IsNullOrEmpty(user.PasswordHash)
            || !_passwordHasher.IsCorrectPassword(user, user.PasswordHash, request.Password))
        {
            return Result.Failure<AuthResponse>(InvalidCredentials);
        }

        var session = AuthSession.Create(
            user.Id,
            clientId,
            request.DeviceName,
            user.SecurityStamp,
            now,
            TimeSpan.FromDays(_options.IdleExpirationDays),
            TimeSpan.FromDays(_options.AbsoluteExpirationDays));
        string rawToken = _tokenService.Generate();
        _tokenService.TryHash(rawToken, out var hash);
        var token = RefreshToken.Create(session.Id, hash, now, session.IdleExpiresAt);
        session.SetInitialToken(token.Id);
        await _sessions.AddAsync(session, cancellationToken);
        await _tokens.AddAsync(token, cancellationToken);
        var response = await _responses.CreateAsync(user, session, rawToken, now, cancellationToken);
        _logger.LogInformation("Creating authentication session {SessionId}", session.Id);
        return Result.Success(response);
    }
}
