using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BookingSystem.HttpClients;

internal static class RequestBuilder
{
    public static HttpRequestMessage Build(HttpApiRequest request, HttpApiClientContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Method);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Path);
        if (request.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("An explicit correlation ID must be nonempty.", nameof(request));
        }

        string path = request.Path;
        if (path.StartsWith('/') || path.Any(c => c is '\\' or '?' or '#' || char.IsControl(c))
            || Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Path must be relative to the dependency base address; supply query values separately.", nameof(request));
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        path = Regex.Replace(path, "\\{([A-Za-z][A-Za-z0-9_]*)\\}", match =>
        {
            string key = match.Groups[1].Value;
            if (!request.RouteValues.TryGetValue(key, out string? value) || value is null)
            {
                throw new ArgumentException("A route placeholder has no value.", nameof(request));
            }

            used.Add(key);
            return Uri.EscapeDataString(value);
        });
        if (path.Contains('{') || path.Contains('}') || request.RouteValues.Keys.Any(key => !used.Contains(key)))
        {
            throw new ArgumentException("Route placeholders and values must match.", nameof(request));
        }

        string query = string.Join('&', request.QueryParameters.Where(pair => pair.Value is not null)
            .Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value!)));
        var baseUri = new Uri(context.Options.BaseAddress.TrimEnd('/') + "/");
        var uri = new Uri(baseUri, path + (query.Length == 0 ? string.Empty : "?" + query));
        if (uri.Scheme != baseUri.Scheme || uri.Host != baseUri.Host || uri.Port != baseUri.Port
            || uri.UserInfo.Length > 0 || uri.Fragment.Length > 0)
        {
            throw new ArgumentException("Request cannot override the dependency authority.", nameof(request));
        }

        var message = new HttpRequestMessage(request.Method, uri);
        try
        {
            foreach (var header in request.Headers)
            {
                if (!HeaderRules.IsName(header.Key) || !HeaderRules.IsValue(header.Value)
                    || HeaderRules.IsProtected(header.Key, context.Options.ApiKeyHeaderName))
                {
                    throw new ArgumentException("A request header is invalid or reserved.", nameof(request));
                }

                message.Headers.Add(header.Key, header.Value);
            }

            if (request.Body is not null)
            {
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request.Body, request.Body.GetType(), context.Options.JsonOptions);
                message.Content = new ByteArrayContent(payload);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            }

            return message;
        }
        catch
        {
            message.Dispose();
            throw;
        }
    }
}
