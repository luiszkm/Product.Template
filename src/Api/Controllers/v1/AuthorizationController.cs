using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Configurations;
using Product.Template.Core.Authorization.Application.Handlers.Permission.Commands;
using Product.Template.Core.Authorization.Application.Handlers.Role.Commands;
using Product.Template.Core.Authorization.Application.Handlers.UserAssignment.Commands;
using Product.Template.Core.Authorization.Application.Outputs;
using Product.Template.Core.Authorization.Application.Queries.Permission;
using Product.Template.Core.Authorization.Application.Queries.Role;
using Product.Template.Core.Authorization.Application.Queries.UserAssignment;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/authorization")]
[Produces("application/json")]
[Tags("Authorization")]
public class AuthorizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =================== ROLES ===================

    [HttpGet("roles")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<RoleOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<RoleOutput>>> ListRoles(
        [FromQuery] ListRolesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{id:guid}", Name = nameof(GetRoleById))]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesReadPolicy)]
    [ProducesResponseType(typeof(RoleWithPermissionsOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleWithPermissionsOutput>> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("roles")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoleOutput>> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRoleById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("roles/{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleOutput>> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (id != command.RoleId)
            return BadRequest("ID in URL must match RoleId in the request body.");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("roles/{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }

    // =================== ROLE PERMISSIONS ===================

    [HttpGet("roles/{id:guid}/permissions")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesReadPolicy)]
    [ProducesResponseType(typeof(RoleWithPermissionsOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleWithPermissionsOutput>> GetRolePermissions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("roles/{id:guid}/permissions")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignPermissionToRole(Guid id, [FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignPermissionToRoleCommand(id, request.PermissionId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("roles/{id:guid}/permissions/{permissionId:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePermissionFromRole(Guid id, Guid permissionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokePermissionFromRoleCommand(id, permissionId), cancellationToken);
        return NoContent();
    }

    // =================== PERMISSIONS ===================

    [HttpGet("permissions")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationPermissionsReadPolicy)]
    [ProducesResponseType(typeof(PaginatedListOutput<PermissionOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedListOutput<PermissionOutput>>> ListPermissions(
        [FromQuery] ListPermissionsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("permissions")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationPermissionsManagePolicy)]
    [ProducesResponseType(typeof(PermissionOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PermissionOutput>> CreatePermission([FromBody] CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("permissions/{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationPermissionsManagePolicy)]
    [ProducesResponseType(typeof(PermissionOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionOutput>> UpdatePermission(Guid id, [FromBody] UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.PermissionId)
            return BadRequest("ID in URL must match PermissionId in the request body.");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("permissions/{id:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationPermissionsManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePermissionCommand(id), cancellationToken);
        return NoContent();
    }

    // =================== USER ASSIGNMENTS ===================

    [HttpGet("users/{userId:guid}/roles")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesReadPolicy)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RoleOutput>>> GetUserAssignments(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserAssignmentsQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/roles")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignUserToRole(Guid userId, [FromBody] AssignUserRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignUserToRoleCommand(userId, request.RoleId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = SecurityConfiguration.AuthorizationRolesManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserFromRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeUserFromRoleCommand(userId, roleId), cancellationToken);
        return NoContent();
    }

    public sealed record AssignPermissionRequest(Guid PermissionId);
    public sealed record AssignUserRequest(Guid RoleId);
}
