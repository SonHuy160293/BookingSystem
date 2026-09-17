using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.CreateScreeningRoom;
using BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.UpdateScreeningRoom;
using BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRoomById;
using BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Cinema.API.Controllers;

public sealed class ScreeningRoomsController : ApiController
{
    private const string GetByIdActionName = "GetScreeningRoomById";

    public ScreeningRoomsController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ScreeningRoomListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetScreeningRoomsAsync([FromQuery] GetScreeningRoomsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetScreeningRoomsQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetByIdActionName)]
    [ProducesResponseType(typeof(ScreeningRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScreeningRoomByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetScreeningRoomByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScreeningRoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateScreeningRoomAsync([FromBody] CreateScreeningRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateScreeningRoomCommand(request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        // Read after the command pipeline commits so audit timestamps reflect persisted state.
        var persisted = await Sender.SendAsync(new GetScreeningRoomByIdQuery(result.Value.Id), cancellationToken);
        return persisted.IsSuccess
            ? CreatedAtAction(GetByIdActionName, new { id = result.Value.Id, version = RouteData.Values["version"] }, persisted.Value)
            : HandleFailure(persisted);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ScreeningRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateScreeningRoomAsync(Guid id, [FromBody] UpdateScreeningRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new UpdateScreeningRoomCommand(id, request), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var persisted = await Sender.SendAsync(new GetScreeningRoomByIdQuery(id), cancellationToken);
        return persisted.IsSuccess ? Ok(persisted.Value) : HandleFailure(persisted);
    }
}
