using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Application.Features.Auth.Commands.CleanupSessions;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence;
using BookingSystem.SharedKernel.Cqrs;
using BookingSystem.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BookingSystem.Identity.Tests;

[Collection("Identity SQL")]
public sealed class AuthFlowSqlTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IdentitySqlFixture _fixture;
    public AuthFlowSqlTests(IdentitySqlFixture fixture) => _fixture = fixture;
    [SqlFact]
    public async Task LoginPersistsOnlyHashesAndReturnsSessionBoundShortLivedJwtAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        Assert.Equal(43, login.RefreshToken.Length);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Equal(login.SessionId.ToString(), jwt.Claims.Single(claim => claim.Type == CustomClaims.SessionId).Value);
        Assert.InRange(login.AccessTokenExpiresAt - _fixture.Clock.GetUtcNow(), TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));
        await using var db = _fixture.CreateDbContext();
        var session = await db.AuthSessions.SingleAsync(item => item.Id == login.SessionId);
        var token = await db.RefreshTokens.SingleAsync(item => item.SessionId == session.Id);
        Assert.Equal(token.Id, session.CurrentRefreshTokenId);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal(DateTimeKind.Utc, session.CreatedAt.Kind);
        Assert.Equal(TimeSpan.FromDays(7), session.IdleExpiresAt - session.CreatedAt);
        Assert.Equal(TimeSpan.FromDays(30), session.AbsoluteExpiresAt - session.CreatedAt);
        Assert.Equal(8, session.RowVersion.Length);
        var generator = new BookingSystem.Identity.Infrastructure.Services.RefreshTokenService();
        Assert.True(generator.TryHash(login.RefreshToken, out var hash));
        Assert.Equal(hash, token.TokenHash);
        Assert.DoesNotContain(_fixture.Logs.Messages, message => message.Contains(login.RefreshToken, StringComparison.Ordinal));
        Assert.DoesNotContain(_fixture.Logs.Messages, message => message.Contains(login.AccessToken, StringComparison.Ordinal));
    }

    [SqlFact]
    public async Task RotationAndReplayCommitRevocationWithoutAffectingAnotherDeviceAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var first = await LoginAsync(user.Email);
        var other = await LoginAsync(user.Email);
        var rotated = await RefreshAsync(first.RefreshToken);
        Assert.Equal(first.SessionId, rotated.SessionId);
        Assert.NotEqual(first.RefreshToken, rotated.RefreshToken);
        await using (var db = _fixture.CreateDbContext())
        {
            var tokens = await db.RefreshTokens.Where(token => token.SessionId == first.SessionId).ToListAsync();
            Assert.Equal(2, tokens.Count);
            var consumed = Assert.Single(tokens, token => token.ConsumedAt is not null);
            Assert.Equal(Assert.Single(tokens, token => token.ConsumedAt is null).Id, consumed.ReplacedByTokenId);
        }

        using var replay = await PostTokenAsync("refresh", first.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var successor = await PostTokenAsync("refresh", rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, successor.StatusCode);
        Assert.NotNull(await RefreshAsync(other.RefreshToken));
        await using var database = _fixture.CreateDbContext();
        Assert.Equal("Replay", (await database.AuthSessions.SingleAsync(session => session.Id == first.SessionId)).RevokedReason);
        Assert.Null((await database.AuthSessions.SingleAsync(session => session.Id == other.SessionId)).RevokedAt);
    }

    [SqlFact]
    public async Task SimultaneousRefreshesHaveOneWinnerAndRevokeTheFamilyAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var responses = await Task.WhenAll(PostTokenAsync("refresh", login.RefreshToken), PostTokenAsync("refresh", login.RefreshToken));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            var winner = await ReadDataAsync<AuthResponse>(responses.Single(response => response.StatusCode == HttpStatusCode.OK));
            using var rejected = await PostTokenAsync("refresh", winner.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            await using var db = _fixture.CreateDbContext();
            Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.SessionId == login.SessionId));
            Assert.Equal("Replay", (await db.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedReason);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [SqlFact]
    public async Task SessionsRequireAuthenticationAndEnforceOwnershipAsync()
    {
        using var client = _fixture.Factory!.CreateClient();
        foreach (var request in new[]
        {
            new HttpRequestMessage(HttpMethod.Get, "/api/Auth/sessions"),
            new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout-all"),
            new HttpRequestMessage(HttpMethod.Delete, "/api/Auth/sessions/" + Guid.NewGuid())
        }

        )
        {
            using (request)
            using (var response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        var user = await _fixture.CreateUserAsync();
        var owner = await LoginAsync(user.Email);
        var device = await LoginAsync(user.Email);
        var stranger = await _fixture.CreateUserAsync();
        var strangerLogin = await LoginAsync(stranger.Email);
        using (var list = await SendAuthenticatedAsync(HttpMethod.Get, "sessions", owner.AccessToken))
        {
            var sessions = await ReadDataAsync<AuthSessionDto[]>(list);
            Assert.Equal(2, sessions.Length);
            Assert.Equal(owner.SessionId, Assert.Single(sessions, session => session.IsCurrent).SessionId);
            Assert.DoesNotContain(sessions, session => session.SessionId == strangerLogin.SessionId);
        }

        using (var forbidden = await SendAuthenticatedAsync(HttpMethod.Delete, "sessions/" + device.SessionId, strangerLogin.AccessToken))
        {
            Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        }

        using (var revoked = await SendAuthenticatedAsync(HttpMethod.Delete, "sessions/" + device.SessionId, owner.AccessToken))
        {
            Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        }

        using (var repeated = await SendAuthenticatedAsync(HttpMethod.Delete, "sessions/" + device.SessionId, owner.AccessToken))
        {
            Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        }

        using var refresh = await PostTokenAsync("refresh", device.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.NotNull(await RefreshAsync(owner.RefreshToken));
    }

    [SqlFact]
    public async Task LogoutWorksWithExpiredAccessAndConsumedRefreshTokensAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var next = await RefreshAsync(login.RefreshToken);
        _fixture.Clock.Set(_fixture.Clock.GetUtcNow().AddMinutes(10));
        using var logout = await PostTokenAsync("logout", login.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var repeated = await PostTokenAsync("logout", login.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        using var unknown = await PostTokenAsync("logout", new BookingSystem.Identity.Infrastructure.Services.RefreshTokenService().Generate());
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
        using var refresh = await PostTokenAsync("refresh", next.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [SqlFact]
    public async Task LogoutAllRevokesExistingSessionsAndAllowsLaterLoginAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var first = await LoginAsync(user.Email);
        var other = await LoginAsync(user.Email);
        using var logout = await SendAuthenticatedAsync(HttpMethod.Post, "logout-all", first.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        foreach (var token in new[]
        {
            first.RefreshToken,
            other.RefreshToken
        }

        )
        {
            using var response = await PostTokenAsync("refresh", token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var later = await LoginAsync(user.Email);
        Assert.NotNull(await RefreshAsync(later.RefreshToken));
    }

    [SqlFact]
    public async Task RefreshAndLogoutRaceCannotEscapeRevocationAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var responses = await Task.WhenAll(PostTokenAsync("refresh", login.RefreshToken), PostTokenAsync("logout", login.RefreshToken));
        try
        {
            Assert.Equal(HttpStatusCode.NoContent, responses[1].StatusCode);
            Assert.Contains(responses[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
            if (responses[0].StatusCode == HttpStatusCode.OK)
            {
                var rotated = await ReadDataAsync<AuthResponse>(responses[0]);
                using var refresh = await PostTokenAsync("refresh", rotated.RefreshToken);
                Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
            }

            await using var db = _fixture.CreateDbContext();
            Assert.NotNull((await db.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedAt);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [SqlFact]
    public async Task RefreshRacesWithLogoutAllAndIndividualRevocationAsync()
    {
        foreach (bool all in new[]
        {
            false,
            true
        }

        )
        {
            var user = await _fixture.CreateUserAsync();
            var login = await LoginAsync(user.Email);
            var responses = await Task.WhenAll(
                PostTokenAsync("refresh", login.RefreshToken),
                SendAuthenticatedAsync(
                all ? HttpMethod.Post : HttpMethod.Delete,
                all ? "logout-all" : "sessions/" + login.SessionId,
                login.AccessToken));
            try
            {
                Assert.Equal(HttpStatusCode.NoContent, responses[1].StatusCode);
                Assert.Contains(responses[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
                await using var db = _fixture.CreateDbContext();
                Assert.NotNull((await db.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedAt);
            }
            finally
            {
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            }
        }
    }

    [SqlFact]
    public async Task IdleAndAbsoluteExpiryCannotBeExtendedPastTheirLimitsAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var start = _fixture.Clock.GetUtcNow();
        _fixture.Clock.Set(start.AddDays(7));
        using (var expired = await PostTokenAsync("refresh", login.RefreshToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        }

        var another = await LoginAsync(user.Email);
        start = _fixture.Clock.GetUtcNow();
        foreach (int day in new[]
        {
            6,
            12,
            18,
            24,
            29
        }

        )
        {
            _fixture.Clock.Set(start.AddDays(day));
            another = await RefreshAsync(another.RefreshToken);
        }

        Assert.Equal(start.AddDays(30), another.RefreshTokenExpiresAt);
        _fixture.Clock.Set(start.AddDays(30));
        using var absolute = await PostTokenAsync("refresh", another.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, absolute.StatusCode);
    }

    [SqlFact]
    public async Task ExpiredConsumedTokenStillDetectsReplayInAnActiveFamilyAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var start = _fixture.Clock.GetUtcNow();
        _fixture.Clock.Set(start.AddDays(6));
        var rotated = await RefreshAsync(login.RefreshToken);
        _fixture.Clock.Set(start.AddDays(8));
        using var replay = await PostTokenAsync("refresh", login.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var rejected = await PostTokenAsync("refresh", rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        await using var db = _fixture.CreateDbContext();
        Assert.Equal("Replay", (await db.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedReason);
    }

    [SqlFact]
    public async Task RefreshRejectsIneligibleAccountsAndSecurityStampChangesAsync()
    {
        foreach (string property in new[]
        {
            "IsEnabled",
            "SecurityStamp",
            "TwoFactorEnabled",
            "LockoutEnd"
        }

        )
        {
            var user = await _fixture.CreateUserAsync();
            var login = await LoginAsync(user.Email);
            await using (var db = _fixture.CreateDbContext())
            {
                var entity = await db.Users.SingleAsync(item => item.Id == user.Id);
                if (property == "IsEnabled")
                {
                    db.Entry(entity).Property(property).CurrentValue = false;
                }

                if (property == "SecurityStamp")
                {
                    db.Entry(entity).Property(property).CurrentValue = Guid.NewGuid().ToString();
                }

                if (property == "TwoFactorEnabled")
                {
                    db.Entry(entity).Property(property).CurrentValue = true;
                }

                if (property == "LockoutEnd")
                {
                    db.Entry(entity).Property("LockoutEnabled").CurrentValue = true;
                    db.Entry(entity).Property(property).CurrentValue = DateTimeOffset.UtcNow.AddDays(1);
                }

                await db.SaveChangesAsync();
            }

            using var rejected = await PostTokenAsync("refresh", login.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            await using var database = _fixture.CreateDbContext();
            Assert.NotNull((await database.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedAt);
        }
    }

    [SqlFact]
    public async Task WrongClientAndMalformedInputDoNotRotateOrRevokeAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        using var wrong = await PostTokenAsync("refresh", login.RefreshToken, "other-client");
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using var malformed = await PostTokenAsync("refresh", "malformed");
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.NotNull(await RefreshAsync(login.RefreshToken));
    }

    [SqlFact]
    public async Task RefreshLoadsCurrentRolesAndPermissionsAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        await using (var db = _fixture.CreateDbContext())
        {
            var role = Role.Create(null, "NewRole", "NEWROLE", Guid.NewGuid().ToString());
            db.Roles.Add(role);
            db.RoleClaims.Add(RoleClaim.Create(role.Id, CustomClaims.Permission, "Test.View"));
            db.UserRoles.Add(UserRole.Create(user.Id, role));
            await db.SaveChangesAsync();
        }

        var refreshed = await RefreshAsync(login.RefreshToken);
        Assert.Contains("NewRole", refreshed.User.Roles!);
        Assert.Contains("Test.View", refreshed.User.Permissions!);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshed.AccessToken);
        Assert.Contains(jwt.Claims, claim => claim.Type == CustomClaims.Permission && claim.Value == "Test.View");
    }

    [SqlFact]
    public async Task FailedLoginCommitLeavesNoSessionOrRefreshTokenAsync()
    {
        var user = await _fixture.CreateUserAsync();
        _fixture.Faults.FailBeforeCommit = true;
        using var client = _fixture.Factory!.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest(user.Email, "TestPassword!2026"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("refreshToken", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.AuthSessions.AnyAsync(session => session.UserId == user.Id));
    }

    [SqlFact]
    public async Task RolledBackRotationDoesNotPartiallyConsumeOrInsertTokensAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        _fixture.Faults.FailBeforeCommit = true;
        using var failed = await PostTokenAsync("refresh", login.RefreshToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.DoesNotContain("accessToken", await failed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using (var db = _fixture.CreateDbContext())
        {
            Assert.Single(await db.RefreshTokens.Where(token => token.SessionId == login.SessionId).ToListAsync());
            Assert.Null((await db.RefreshTokens.SingleAsync(token => token.SessionId == login.SessionId)).ConsumedAt);
        }

        Assert.NotNull(await RefreshAsync(login.RefreshToken));
    }

    [SqlFact]
    public async Task UncertainCommitDoesNotReplayTheTransactionOrDiscloseTokensAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        int previousAttempts = _fixture.Faults.CommitAttempts;
        _fixture.Faults.FailAfterCommit = true;
        using var failed = await PostTokenAsync("refresh", login.RefreshToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(previousAttempts + 1, _fixture.Faults.CommitAttempts);
        Assert.DoesNotContain("accessToken", await failed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", await failed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using var db = _fixture.CreateDbContext();
        Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.SessionId == login.SessionId));
        Assert.Null((await db.AuthSessions.SingleAsync(session => session.Id == login.SessionId)).RevokedAt);
    }

    [SqlFact]
    public async Task CleanupKeepsHistoryUntilAbsoluteExpiryPlusRetentionAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var first = await LoginAsync(user.Email);
        var start = _fixture.Clock.GetUtcNow();
        var rotated = await RefreshAsync(first.RefreshToken);
        _fixture.Clock.Set(start.AddDays(36));
        await CleanupAsync(user.Id);
        await using (var db = _fixture.CreateDbContext())
        {
            Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.SessionId == first.SessionId));
        }

        _fixture.Clock.Set(start.AddDays(37));
        var newer = await LoginAsync(user.Email);
        await CleanupAsync(user.Id);
        await using var database = _fixture.CreateDbContext();
        Assert.False(await database.AuthSessions.AnyAsync(session => session.Id == first.SessionId));
        Assert.False(await database.RefreshTokens.AnyAsync(token => token.SessionId == first.SessionId));
        Assert.True(await database.AuthSessions.AnyAsync(session => session.Id == newer.SessionId));
    }

    [SqlFact]
    public async Task CleanupBoundsTokenRowsAndDrainsLargeTerminalFamiliesAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        var start = _fixture.Clock.GetUtcNow();
        await using (var db = _fixture.CreateDbContext())
        {
            for (int index = 0; index < 1001; index++)
            {
                db.RefreshTokens.Add(RefreshToken.Create(
                    login.SessionId,
                    System.Security.Cryptography.SHA256.HashData(BitConverter.GetBytes(index)),
                    start.UtcDateTime,
                    start.AddDays(7).UtcDateTime));
            }

            await db.SaveChangesAsync();
        }

        _fixture.Clock.Set(start.AddDays(37));
        await CleanupAsync(user.Id);
        await using (var db = _fixture.CreateDbContext())
        {
            Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.SessionId == login.SessionId));
            Assert.True(await db.AuthSessions.AnyAsync(session => session.Id == login.SessionId));
        }

        await CleanupAsync(user.Id);
        await using var database = _fixture.CreateDbContext();
        Assert.False(await database.AuthSessions.AnyAsync(session => session.Id == login.SessionId));
        Assert.False(await database.RefreshTokens.AnyAsync(token => token.SessionId == login.SessionId));
    }

    [SqlFact]
    public async Task RowversionRejectsStaleUpdatesOutsideTheLockProtocolAsync()
    {
        var user = await _fixture.CreateUserAsync();
        var login = await LoginAsync(user.Email);
        await using var staleDatabase = _fixture.CreateDbContext();
        var stale = await staleDatabase.AuthSessions.SingleAsync(session => session.Id == login.SessionId);
        using var logout = await PostTokenAsync("logout", login.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        stale.Revoke(_fixture.Clock.GetUtcNow().UtcDateTime, "Replay");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDatabase.SaveChangesAsync());
    }

    private async Task CleanupAsync(Guid userId)
    {
        await using var scope = _fixture.Factory!.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new CleanupSessionsCommand(userId));
    }

    private async Task<AuthResponse> LoginAsync(string email)
    {
        using var client = _fixture.Factory!.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest(email, "TestPassword!2026"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Contains(response.Headers.Pragma, item => item.Name == "no-cache");
        return await ReadDataAsync<AuthResponse>(response);
    }

    private async Task<AuthResponse> RefreshAsync(string token)
    {
        using var response = await PostTokenAsync("refresh", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        return await ReadDataAsync<AuthResponse>(response);
    }

    private async Task<HttpResponseMessage> PostTokenAsync(string endpoint, string token, string clientId = "booking-system")
    {
        using var client = _fixture.Factory!.CreateClient();
        return await client.PostAsJsonAsync("/api/Auth/" + endpoint, new RefreshTokenRequest(token, clientId));
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpMethod method, string endpoint, string accessToken)
    {
        using var client = _fixture.Factory!.CreateClient();
        using var request = new HttpRequestMessage(method, "/api/Auth/" + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("data").Deserialize<T>(JsonOptions)!;
    }
}
