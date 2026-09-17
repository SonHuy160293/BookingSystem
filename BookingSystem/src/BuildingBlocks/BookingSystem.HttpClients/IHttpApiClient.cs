namespace BookingSystem.HttpClients;

public interface IHttpApiClient
{
    Task<HttpCallResult<TResponse>> SendAsync<TResponse>(HttpApiRequest request, CancellationToken cancellationToken);

    Task<HttpCallResult> SendAsync(HttpApiRequest request, CancellationToken cancellationToken);
}
