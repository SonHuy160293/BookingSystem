using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Application.Features.Branches.Commands.CreateBranch;
using BookingSystem.Identity.Application.Features.Branches.Commands.UpdateBranch;
using BookingSystem.Identity.Application.Features.Branches.Queries.GetBranchById;
using BookingSystem.Identity.Application.Features.Branches.Queries.GetBranches;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class BranchesController : ApiController
{
    private const string GetBranchByIdActionName = "GetBranchById";

    public BranchesController(ISender sender)
        : base(sender)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BranchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBranchesAsync([FromQuery] GetBranchesRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetBranchesQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetBranchByIdActionName)]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetBranchByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBranchAsync([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateBranchCommand(request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(
                GetBranchByIdActionName,
                new { id = result.Value.Id, version = RouteData.Values["version"] },
                result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranchAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new UpdateBranchCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
