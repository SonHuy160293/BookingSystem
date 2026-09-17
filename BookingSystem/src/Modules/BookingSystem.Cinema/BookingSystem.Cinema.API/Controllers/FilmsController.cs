using BookingSystem.AspNetCore.Controllers;
using BookingSystem.AspNetCore.Security;
using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.Cinema.Application.Features.Films.Commands.CreateFilm;
using BookingSystem.Cinema.Application.Features.Films.Commands.UpdateFilm;
using BookingSystem.Cinema.Application.Features.Films.Queries.GetFilms;
using BookingSystem.Cinema.Application.Features.Films.Queries.GetFilmById;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using BookingSystem.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Cinema.API.Controllers;

[Authorize]
public sealed class FilmsController : ApiController
{
    private const string GetByIdActionName = "GetFilmById";

    public FilmsController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [PermissionAuthorize(Functions.Films, ActionType.View)]
    [ProducesResponseType(typeof(PagedResult<FilmListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFilmsAsync([FromQuery] GetFilmsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetFilmsQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetByIdActionName)]
    [PermissionAuthorize(Functions.Films, ActionType.View)]
    [ProducesResponseType(typeof(FilmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFilmByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetFilmByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [PermissionAuthorize(Functions.Films, ActionType.Create)]
    [ProducesResponseType(typeof(FilmDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateFilmAsync([FromBody] CreateFilmRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateFilmCommand(request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        // Read after the command pipeline commits so audit timestamps reflect persisted state.
        var persisted = await Sender.SendAsync(new GetFilmByIdQuery(result.Value.Id, ActionType.Create), cancellationToken);
        return persisted.IsSuccess
            ? CreatedAtAction(GetByIdActionName, new { id = result.Value.Id, version = RouteData.Values["version"] }, persisted.Value)
            : HandleFailure(persisted);
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Functions.Films, ActionType.Update)]
    [ProducesResponseType(typeof(FilmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFilmAsync(Guid id, [FromBody] UpdateFilmRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new UpdateFilmCommand(id, request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var persisted = await Sender.SendAsync(new GetFilmByIdQuery(id, ActionType.Update), cancellationToken);
        return persisted.IsSuccess ? Ok(persisted.Value) : HandleFailure(persisted);
    }
}
