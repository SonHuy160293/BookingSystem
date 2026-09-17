using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.Identity.Application.Features.Roles.Commands.CreateRole;
using BookingSystem.Identity.Application.Features.Roles.Commands.AddRolePermission;
using BookingSystem.Identity.Application.Features.Roles.Queries.GetRoleById;
using BookingSystem.Identity.Application.Features.Roles.Queries.GetRoles;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class RolesController : ApiController
{
    private const string GetRoleByIdActionName = "GetRoleById";

    public RolesController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRolesAsync([FromQuery] GetRolesRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetRolesQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetRoleByIdActionName)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetRoleByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateRoleCommand(request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(
                GetRoleByIdActionName,
                new { id = result.Value.Id, version = RouteData.Values["version"] },
                result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRolePermissionAsync([FromRoute] Guid id, [FromBody] PermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new AddRolePermissionCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
}
