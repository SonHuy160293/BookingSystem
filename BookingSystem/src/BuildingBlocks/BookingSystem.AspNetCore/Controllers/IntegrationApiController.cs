using Asp.Versioning;
using BookingSystem.AspNetCore.Security;
using BookingSystem.AspNetCore.Controllers;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.AspNetCore.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ServiceFilter(typeof(IntegrationApiKeyFilter))]
[Route("api/v{version:apiVersion}/integrations/[controller]")]
[Produces("application/json")]
public abstract class IntegrationApiController : ControllerBase
{
    protected IntegrationApiController(ISender sender)
    {
        Sender = sender;
    }

    protected ISender Sender { get; }

    protected ActionResult HandleFailure(Result result)
        => result.Error == Error.None
            ? BadRequest()
            : BadRequest(result.Error);
}
