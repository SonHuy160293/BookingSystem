using System.Net;

namespace BookingSystem.HttpClients;

public sealed class HttpCallResult
{
    internal HttpCallResult(HttpStatusCode? statusCode, bool hasBody, HttpCallFailure? failure)
    {
        StatusCode = statusCode;
        HasBody = hasBody;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;

    public HttpStatusCode? StatusCode { get; }

    public bool HasBody { get; }

    public HttpCallFailure? Failure { get; }
}

public sealed class HttpCallResult<T>
{
    internal HttpCallResult(HttpStatusCode? statusCode, T? data, bool hasBody, HttpCallFailure? failure)
    {
        StatusCode = statusCode;
        Data = data;
        HasBody = hasBody;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;

    public HttpStatusCode? StatusCode { get; }

    public T? Data { get; }

    public bool HasBody { get; }

    public HttpCallFailure? Failure { get; }
}
