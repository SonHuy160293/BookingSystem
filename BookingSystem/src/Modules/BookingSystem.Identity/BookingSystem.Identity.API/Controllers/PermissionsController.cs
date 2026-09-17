using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Permissions;
using BookingSystem.Identity.Application.Features.Permissions.Queries.GetPermissions;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class PermissionsController : ApiController
{
    public PermissionsController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPermissionsAsync([FromQuery] GetPermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetPermissionsQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
