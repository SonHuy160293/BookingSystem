using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace BookingSystem.HttpClients;

public abstract class JsonApiClient : IHttpApiClient
{
    private readonly HttpClient _client;
    private readonly HttpApiClientContext _context;

    protected JsonApiClient(HttpClient client, HttpApiClientContext context)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(context);
        _client = client;
        _context = context;
    }

    public Task<HttpCallResult<TResponse>> SendAsync<TResponse>(HttpApiRequest request, CancellationToken cancellationToken)
        => SendCoreAsync<TResponse>(request, true, cancellationToken);

    public async Task<HttpCallResult> SendAsync(HttpApiRequest request, CancellationToken cancellationToken)
    {
        var result = await SendCoreAsync<object>(request, false, cancellationToken).ConfigureAwait(false);
        return new HttpCallResult(result.StatusCode, result.HasBody, result.Failure);
    }

    private async Task<HttpCallResult<T>> SendCoreAsync<T>(HttpApiRequest request, bool readJson, CancellationToken callerToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        callerToken.ThrowIfCancellationRequested();
        long started = Stopwatch.GetTimestamp();
        HttpStatusCode? status = null;
        bool hasBody = false;
        Guid? correlationId = null;
        string outcome = "InvalidRequest";
        using var deadline = new CancellationTokenSource(_context.Options.TotalTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken, deadline.Token);
        CancellationToken token = linked.Token;
        try
        {
            using var message = RequestBuilder.Build(request, _context);
            token.ThrowIfCancellationRequested();
            if (_context.IsInternal || _context.Options.ForwardCorrelationId)
            {
                correlationId = request.CorrelationId ?? _context.CorrelationIdAccessor?.GetCorrelationId();
                if (correlationId.HasValue && correlationId != Guid.Empty)
                {
                    message.Headers.Add("X-Correlation-Id", correlationId.Value.ToString("D"));
                }
                else
                {
                    correlationId = null;
                }
            }

            if (_context.IsInternal)
            {
                message.Headers.Add("X-Integration-Key", _context.Options.IntegrationKey!);
            }
            else if (_context.Options.ApiKey is not null)
            {
                message.Headers.Add(_context.Options.ApiKeyHeaderName!, _context.Options.ApiKey);
            }

            if (_context.Options.UseServiceToken)
            {
                string? accessToken;
                try
                {
                    accessToken = await _context.TokenProvider!.GetAccessTokenAsync(_context.DependencyName, token)
                        .AsTask().WaitAsync(token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    return Failure(HttpCallFailureKind.AuthenticationFailure);
                }

                if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
                {
                    return Failure(HttpCallFailureKind.AuthenticationFailure);
                }

                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            status = response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                return Failure(HttpCallFailureKind.HttpFailure, ReadRetryAfter(response));
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            hasBody = bytes.Length > 0;
            T? data = default;
            if (readJson && hasBody)
            {
                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not null && !mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                    && !mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
                {
                    return Failure(HttpCallFailureKind.InvalidResponse);
                }

                try
                {
                    using var stream = new MemoryStream(bytes, writable: false);
                    data = await JsonSerializer.DeserializeAsync<T>(stream, _context.Options.JsonOptions, token).ConfigureAwait(false);
                }
                catch (JsonException)
                {
                    return Failure(HttpCallFailureKind.InvalidResponse);
                }
                catch (NotSupportedException)
                {
                    return Failure(HttpCallFailureKind.InvalidResponse);
                }
            }

            token.ThrowIfCancellationRequested();
            outcome = "None";
            return new HttpCallResult<T>(status, data, hasBody, null);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            outcome = "Cancelled";
            throw;
        }
        catch (OperationCanceledException)
        {
            return Failure(HttpCallFailureKind.Timeout);
        }
        catch (TimeoutRejectedException)
        {
            return Failure(HttpCallFailureKind.Timeout);
        }
        catch (BrokenCircuitException)
        {
            return Failure(HttpCallFailureKind.CircuitOpen);
        }
        catch (HttpRequestException)
        {
            return Failure(HttpCallFailureKind.NetworkFailure);
        }
        catch (IOException)
        {
            return Failure(HttpCallFailureKind.NetworkFailure);
        }
        finally
        {
            string method = request.Method?.Method ?? "OTHER";
            if (method is not ("GET" or "HEAD" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS"))
            {
                method = "OTHER";
            }

            _context.Logger.LogInformation(HttpClientTelemetry.CompletionTemplate,
                _context.DependencyName, method, status.HasValue ? (int)status.Value : 0,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, outcome, correlationId?.ToString("D") ?? "None");
        }

        HttpCallResult<T> Failure(HttpCallFailureKind kind, TimeSpan? retryAfter = null)
        {
            if (callerToken.IsCancellationRequested)
            {
                outcome = "Cancelled";
                callerToken.ThrowIfCancellationRequested();
            }

            if (deadline.IsCancellationRequested)
            {
                kind = HttpCallFailureKind.Timeout;
            }

            outcome = kind.ToString();
            return new HttpCallResult<T>(status, default, hasBody, HttpCallFailure.Create(kind, retryAfter));
        }
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        TimeSpan? delay = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
        return delay.HasValue ? (delay.Value < TimeSpan.Zero ? TimeSpan.Zero : delay.Value) : null;
    }
}
