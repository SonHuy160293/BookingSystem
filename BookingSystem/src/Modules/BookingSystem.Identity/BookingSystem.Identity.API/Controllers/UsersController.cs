using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Application.Features.Users.Commands.AddUserRole;
using BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;
using BookingSystem.Identity.Application.Features.Users.Queries.GetUserById;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class UsersController : ApiController
{
    private const string GetUserByIdActionName = "GetUserById";

    public UsersController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetUserByIdActionName)]
    [ProducesResponseType(typeof(AccountUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateUserCommand(request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(
                GetUserByIdActionName,
                new { id = result.Value.Id, version = RouteData.Values["version"] },
                result.Value)
            : HandleFailure(result);
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUserRoleAsync(
        [FromRoute] Guid id,
        [FromBody] RoleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new AddUserRoleCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
}
