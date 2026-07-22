using System.Security.Claims;
using Kartly.Application.Auth;
using Kartly.Application.Products; // PagedResult<T>
using Kartly.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

/// <summary>Admin-only user administration: list, view, change role, activate/deactivate.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminUsersController(IUserAdminService users) : ControllerBase
{
    /// <summary>Returns a searchable, paginated list of users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll(
        [FromQuery] UserQueryParameters query, CancellationToken ct)
        => Ok(await users.GetUsersAsync(query, ct));

    /// <summary>Returns a single user by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(string id, CancellationToken ct)
    {
        try
        {
            return Ok(await users.GetUserByIdAsync(id, ct));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Replaces a user's role.</summary>
    [HttpPut("{id}/role")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> ChangeRole(
        string id, ChangeRoleRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await users.ChangeRoleAsync(id, request.Role, ActingUserId(), ct));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UserAdminException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Activates or deactivates a user.</summary>
    [HttpPut("{id}/active")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> SetActive(
        string id, SetActiveRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await users.SetActiveAsync(id, request.IsActive, ActingUserId(), ct));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UserAdminException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>The signed-in admin's user id (JWT sub), used for the self-guards.</summary>
    private string ActingUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
}
