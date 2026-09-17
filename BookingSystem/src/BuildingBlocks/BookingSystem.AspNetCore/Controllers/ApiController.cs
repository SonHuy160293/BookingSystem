using Asp.Versioning;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.AspNetCore.Controllers;

[ApiController]
[ApiVersion(1.0)]
//[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected ApiController(ISender sender)
    {
        Sender = sender;
    }

    protected ISender Sender { get; }

    protected ActionResult HandleFailure(Result result)
        => result.Error == Error.None
            ? BadRequest()
            : BadRequest(result.Error);
}
