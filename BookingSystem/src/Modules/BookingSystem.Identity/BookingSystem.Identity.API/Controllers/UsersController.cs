using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class UsersController : ApiController
{
    public UsersController(ISender sender) : base(sender)
    {
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request);
        var result = await Sender.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateUser), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }
}
