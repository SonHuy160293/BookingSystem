using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaAccess;
using BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaBranch;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

// Service-to-service contract. The inherited integration filter authenticates the Cinema service.
public sealed class CinemaAccessController : IntegrationApiController
{
    public CinemaAccessController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(CinemaAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAccessAsync(Guid userId, [FromQuery] string permission, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetCinemaAccessQuery(userId, permission), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("branches/{id:guid}")]
    [ProducesResponseType(typeof(BranchAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetBranchAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetCinemaBranchQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
