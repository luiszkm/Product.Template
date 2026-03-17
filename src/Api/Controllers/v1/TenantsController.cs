using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Configurations;
using Product.Template.Core.Tenants.Application.Handlers.Commands;
using Product.Template.Core.Tenants.Application.Outputs;
using Product.Template.Core.Tenants.Application.Queries;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Tags("Tenants")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = SecurityConfiguration.TenantsReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<TenantOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<TenantOutput>>> ListTenants(
        [FromQuery] ListTenantsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}", Name = nameof(GetTenantById))]
    [Authorize(Policy = SecurityConfiguration.TenantsReadPolicy)]
    [ProducesResponseType(typeof(TenantOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantOutput>> GetTenantById(long id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = SecurityConfiguration.TenantsManagePolicy)]
    [ProducesResponseType(typeof(TenantOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantOutput>> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTenantById), new { id = result.TenantId, version = "1.0" }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = SecurityConfiguration.TenantsManagePolicy)]
    [ProducesResponseType(typeof(TenantOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantOutput>> UpdateTenant(long id, [FromBody] UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        if (id != command.TenantId)
            return BadRequest("ID in URL must match TenantId in the request body.");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = SecurityConfiguration.TenantsManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTenant(long id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateTenantCommand(id), cancellationToken);
        return NoContent();
    }
}
