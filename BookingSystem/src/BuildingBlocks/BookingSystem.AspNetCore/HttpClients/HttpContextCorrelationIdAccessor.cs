using BookingSystem.HttpClients;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.AspNetCore.HttpClients;

public sealed class HttpContextCorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    public Guid? GetCorrelationId()
    {
        string? value = accessor.HttpContext?.TraceIdentifier;
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    }
}
