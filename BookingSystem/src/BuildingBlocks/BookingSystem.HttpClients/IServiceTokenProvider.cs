namespace BookingSystem.HttpClients;

/// <summary>A dependency-keyed, thread-safe service credential provider; never forwards user tokens.</summary>
public interface IServiceTokenProvider
{
    ValueTask<string?> GetAccessTokenAsync(string dependencyName, CancellationToken cancellationToken);
}
