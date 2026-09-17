using BookingSystem.AspNetCore.Controllers;
using BookingSystem.AspNetCore.Security;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Application.Features.Cinemas.Commands.CreateCinema;
using BookingSystem.Cinema.Application.Features.Cinemas.Commands.UpdateCinema;
using BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemas;
using BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemaById;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using BookingSystem.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Cinema.API.Controllers;

[Authorize]
public sealed class CinemasController : ApiController
{
    private const string GetByIdActionName = "GetCinemaById";

    public CinemasController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [PermissionAuthorize(Functions.Cinemas, ActionType.View)]
    [ProducesResponseType(typeof(PagedResult<CinemaListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCinemasAsync([FromQuery] GetCinemasRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetCinemasQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetByIdActionName)]
    [PermissionAuthorize(Functions.Cinemas, ActionType.View)]
    [ProducesResponseType(typeof(CinemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCinemaByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetCinemaByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [PermissionAuthorize(Functions.Cinemas, ActionType.Create)]
    [ProducesResponseType(typeof(CinemaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCinemaAsync([FromBody] CreateCinemaRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateCinemaCommand(request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        // Read after the command pipeline commits so audit timestamps reflect persisted state.
        var persisted = await Sender.SendAsync(new GetCinemaByIdQuery(result.Value.Id, ActionType.Create), cancellationToken);
        return persisted.IsSuccess
            ? CreatedAtAction(GetByIdActionName, new { id = result.Value.Id, version = RouteData.Values["version"] }, persisted.Value)
            : HandleFailure(persisted);
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Functions.Cinemas, ActionType.Update)]
    [ProducesResponseType(typeof(CinemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCinemaAsync(Guid id, [FromBody] UpdateCinemaRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new UpdateCinemaCommand(id, request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var persisted = await Sender.SendAsync(new GetCinemaByIdQuery(id, ActionType.Update), cancellationToken);
        return persisted.IsSuccess ? Ok(persisted.Value) : HandleFailure(persisted);
    }
}
