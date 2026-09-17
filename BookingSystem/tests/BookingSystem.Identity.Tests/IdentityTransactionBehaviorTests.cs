using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Application.Behaviors;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Application.Features.Auth.Commands.Login;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Xunit;

namespace BookingSystem.Identity.Tests;

public sealed class IdentityTransactionBehaviorTests
{
    [Fact]
    public async Task AuthenticationCommandsUseTheNonreplayingTransactionAsync()
    {
        var transactions = new RecordingTransactions();
        var behavior = new IdentityTransactionBehavior<LoginCommand, Result<AuthResponse>>(transactions, transactions);
        var expected = Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Invalid credentials."));
        var result = await behavior.HandleAsync(
            new LoginCommand(new LoginRequest("user@example.test", "secret")),
            () => Task.FromResult(expected),
            CancellationToken.None);
        Assert.Same(expected, result);
        Assert.Equal(1, transactions.AuthTransactions);
        Assert.Equal(0, transactions.OrdinaryTransactions);
    }

    [Fact]
    public async Task OrdinaryCommandsKeepTheExistingTransactionPolicyAsync()
    {
        var transactions = new RecordingTransactions();
        var behavior = new IdentityTransactionBehavior<OrdinaryCommand, Result>(transactions, transactions);
        await behavior.HandleAsync(new OrdinaryCommand(), () => Task.FromResult(Result.Success()), CancellationToken.None);
        Assert.Equal(0, transactions.AuthTransactions);
        Assert.Equal(1, transactions.OrdinaryTransactions);
    }

    [Fact]
    public async Task QueriesDoNotOpenTransactionsAsync()
    {
        var transactions = new RecordingTransactions();
        var behavior = new IdentityTransactionBehavior<ReadQuery, Result<string>>(transactions, transactions);
        await behavior.HandleAsync(new ReadQuery(), () => Task.FromResult(Result.Success("result")), CancellationToken.None);
        Assert.Equal(0, transactions.AuthTransactions);
        Assert.Equal(0, transactions.OrdinaryTransactions);
    }

    private sealed class OrdinaryCommand : ICommand { }
    private sealed class ReadQuery : IQuery<string> { }

    private sealed class RecordingTransactions : IUnitOfWork, IAuthUnitOfWork
    {
        public int AuthTransactions { get; private set; }
        public int OrdinaryTransactions { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The behavior must not commit outside the unit of work.");

        public Task<T> ExecuteAuthTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            AuthTransactions++;
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            OrdinaryTransactions++;
            return action(cancellationToken);
        }
    }
}
