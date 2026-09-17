using System.Security.Cryptography;
using System.Text;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Application.Features.Auth.Commands.Login;
using BookingSystem.Identity.Application.Features.Auth.Commands.Refresh;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Services;
using BookingSystem.SharedKernel.Abstractions.Message;
using Xunit;

namespace BookingSystem.Identity.Tests;

public sealed class RefreshTokenTests
{
    private readonly RefreshTokenService _tokens = new();
    [Fact]
    public void TokensAreCanonicalRandom256BitValuesAndHashTheWireValue()
    {
        var tokens = Enumerable.Range(0, 500).Select(_ => _tokens.Generate()).ToArray();
        Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());
        foreach (string token in tokens)
        {
            Assert.Equal(43, token.Length);
            Assert.True(_tokens.TryHash(token, out var hash));
            Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(token)), hash);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("predictable-token")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    [InlineData(" AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void RejectsMalformedAndNoncanonicalTokens(string? token) => Assert.False(_tokens.TryHash(token, out _));
    [Fact]
    public void RotationCapsIdleExpiryAndCannotResurrectRevokedSessions()
    {
        var now = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        var session = AuthSession.Create(Guid.CreateVersion7(), "booking-system", "phone", null, now, TimeSpan.FromDays(7), TimeSpan.FromDays(30));
        var firstId = Guid.CreateVersion7();
        session.SetInitialToken(firstId);
        var nextId = Guid.CreateVersion7();
        session.Rotate(firstId, nextId, now.AddDays(6), TimeSpan.FromDays(7));
        foreach (int day in new[]
        {
            12,
            18,
            24,
            29
        }

        )
        {
            var previousId = session.CurrentRefreshTokenId;
            session.Rotate(previousId, Guid.CreateVersion7(), now.AddDays(day), TimeSpan.FromDays(7));
        }

        Assert.Equal(session.AbsoluteExpiresAt, session.IdleExpiresAt);
        Assert.False(session.IsActive(now.AddDays(30)));
    }

    [Fact]
    public void ExpiryBoundariesAndRevocationAreStrict()
    {
        var now = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        var session = AuthSession.Create(Guid.CreateVersion7(), "booking-system", null, null, now, TimeSpan.FromDays(7), TimeSpan.FromDays(30));
        var token = RefreshToken.Create(session.Id, new byte[32], now, session.IdleExpiresAt);
        session.SetInitialToken(token.Id);
        Assert.False(session.IsActive(session.IdleExpiresAt));
        session.Revoke(now, "Logout");
        Assert.Throws<InvalidOperationException>(() => session.Rotate(token.Id, Guid.CreateVersion7(), now, TimeSpan.FromDays(7)));
        session.Revoke(now.AddMinutes(1), "Replay");
        Assert.Equal("Logout", session.RevokedReason);
        Assert.Throws<InvalidOperationException>(() => token.Consume(token.ExpiresAt, Guid.CreateVersion7()));
    }

    [Fact]
    public async Task ValidatorsRejectMalformedRequestsAsync()
    {
        var refresh = new RefreshCommandValidator(_tokens);
        await Assert.ThrowsAsync<CqrsValidationException>(() => refresh.ValidateAsync(new RefreshCommand(new RefreshTokenRequest("bad", "booking-system")), CancellationToken.None));
        var login = new LoginCommandValidator();
        await Assert.ThrowsAsync<CqrsValidationException>(() => login.ValidateAsync(
            new LoginCommand(new LoginRequest("test@example.com", "password", string.Empty, new string('x', 129))),
            CancellationToken.None));
    }
}
