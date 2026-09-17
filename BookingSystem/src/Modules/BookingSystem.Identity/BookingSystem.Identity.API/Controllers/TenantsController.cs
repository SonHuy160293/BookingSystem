using BookingSystem.AspNetCore.Controllers;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Application.Features.Tenants.Commands.CreateTenant;
using BookingSystem.Identity.Application.Features.Tenants.Commands.UpdateTenant;
using BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenantById;
using BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenants;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Identity.API.Controllers;

public sealed class TenantsController : ApiController
{
    private const string GetTenantByIdActionName = "GetTenantById";

    public TenantsController(ISender sender)
        : base(sender)
    {
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenantAsync([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new CreateTenantCommand(request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(
                GetTenantByIdActionName,
                new { id = result.Value.Id, version = RouteData.Values["version"] },
                result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenantAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new UpdateTenantCommand(id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(GetTenantByIdActionName)]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetTenantByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTenantsAsync([FromQuery] GetTenantsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(new GetTenantsQuery(request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
