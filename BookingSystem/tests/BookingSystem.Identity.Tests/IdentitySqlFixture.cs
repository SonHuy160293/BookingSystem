using System.Collections.Concurrent;
using System.Data.Common;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BookingSystem.Identity.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BOOKING_IDENTITY_TEST_SQL")))
        {
            Skip = "Set BOOKING_IDENTITY_TEST_SQL to a SQL Server connection authorized to create an isolated test database.";
        }
    }
}

[CollectionDefinition("Identity SQL", DisableParallelization = true)]
public sealed class IdentitySqlCollection : ICollectionFixture<IdentitySqlFixture>
{
}

public sealed class TestClock : TimeProvider
{
    private long _utcTicks = DateTimeOffset.UtcNow.UtcTicks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);
    public void Set(DateTimeOffset now) => Interlocked.Exchange(ref _utcTicks, now.UtcTicks);
}

public sealed class TransactionFaults : DbTransactionInterceptor
{
    public bool FailBeforeCommit { get; set; }
    public bool FailAfterCommit { get; set; }
    public int CommitAttempts { get; private set; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        CommitAttempts++;
        if (FailBeforeCommit)
        {
            FailBeforeCommit = false;
            throw new DbUpdateException("Injected transaction failure.");
        }

        return ValueTask.FromResult(result);
    }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (FailAfterCommit)
        {
            FailAfterCommit = false;
            throw new TimeoutException("Injected uncertain commit.");
        }

        return Task.CompletedTask;
    }
}

public sealed class CaptureLogs : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);
    public void Dispose()
    {
    }

    private sealed class CaptureLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages;
        public CaptureLogger(ConcurrentQueue<string> messages) => _messages = messages;
        public IDisposable? BeginScope<TState>(TState state)

    where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => _messages.Enqueue(formatter(state, exception));
    }
}

public sealed class IdentitySqlFixture : IAsyncLifetime
{
    private string? _masterConnection;
    private string? _databaseName;
    public string ConnectionString { get; private set; } = string.Empty;
    public TestClock Clock { get; } = new();
    public TransactionFaults Faults { get; } = new();
    public CaptureLogs Logs { get; } = new();
    public WebApplicationFactory<Program>? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        string? connection = Environment.GetEnvironmentVariable("BOOKING_IDENTITY_TEST_SQL");
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        _databaseName = "BookingIdentityTests_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = "master"
        };
        _masterConnection = builder.ConnectionString;
        await using (var sql = new SqlConnection(_masterConnection))
        {
            await sql.OpenAsync();
            await using var command = sql.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        builder.InitialCatalog = _databaseName;
        ConnectionString = builder.ConnectionString;
        await using (var database = CreateDbContext())
        {
            await database.Database.MigrateAsync();
        }

        Factory = new IdentityFactory(this);
        _ = Factory.Services;
        await using var verificationScope = Factory.Services.CreateAsyncScope();
        Assert.Equal(
            _databaseName,
            verificationScope.ServiceProvider.GetRequiredService<BookingSystemIdentityDbContext>().Database.GetDbConnection().Database);
    }

    public BookingSystemIdentityDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BookingSystemIdentityDbContext>().UseSqlServer(ConnectionString, options => options.EnableRetryOnFailure()).Options);
    public async Task<(Guid Id, string Email)> CreateUserAsync(bool enabled = true)
    {
        Clock.Set(DateTimeOffset.UtcNow);
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        await using var scope = Factory!.Services.CreateAsyncScope();
        var user = User.Create(enabled, "Test User", email, email, "Tester", null);
        user.SetIdentityCredentials(
            email.ToUpperInvariant(),
            email.ToUpperInvariant(),
            scope.ServiceProvider.GetRequiredService<IUserPasswordHasher>().HashPassword(user, "TestPassword!2026"),
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"));
        var database = scope.ServiceProvider.GetRequiredService<BookingSystemIdentityDbContext>();
        database.Users.Add(user);
        await database.SaveChangesAsync();
        return (user.Id, email);
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_masterConnection is not null && _databaseName is not null)
        {
            // Only the fixture-generated database can ever be deleted.
            if (!_databaseName.StartsWith("BookingIdentityTests_", StringComparison.Ordinal) || _databaseName.Length != "BookingIdentityTests_".Length + 32)
            {
                throw new InvalidOperationException("Unexpected test database identifier.");
            }

            SqlConnection.ClearAllPools();
            await using var sql = new SqlConnection(_masterConnection);
            await sql.OpenAsync();
            await using var command = sql.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class IdentityFactory : WebApplicationFactory<Program>
    {
        private readonly IdentitySqlFixture _fixture;
        public IdentityFactory(IdentitySqlFixture fixture) => _fixture = fixture;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ApplicationDb"] = _fixture.ConnectionString, ["AuthSessions:CleanupEnabled"] = "false", ["RateLimiting:AuthPublic:PermitLimit"] = "10000", ["RateLimiting:AuthRefresh:PermitLimit"] = "10000", ["Jwt:AccessTokenExpirationMinutes"] = "5" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(_fixture.Clock);
                services.RemoveAll<DbContextOptions<BookingSystemIdentityDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<BookingSystemIdentityDbContext>>();
                services.AddDbContext<BookingSystemIdentityDbContext>(options => options.UseSqlServer(_fixture.ConnectionString, sql => sql.EnableRetryOnFailure()).AddInterceptors(_fixture.Faults));
                services.RemoveAll<ILoggerFactory>();
                services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Information).AddProvider(_fixture.Logs)));
            });
        }
    }
}
