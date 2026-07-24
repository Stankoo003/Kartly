using System.ComponentModel.DataAnnotations;

namespace Kartly.Application.Users;

/// <summary>
/// Admin-facing view of a user. Deliberately minimal — password hashes, security
/// stamps and other Identity internals are never exposed to clients.
/// </summary>
public sealed record UserResponse(
    string Id,
    string Email,
    string Role,
    bool IsActive);

/// <summary>Query-string parameters for the paginated user list. Bound via <c>[FromQuery]</c>.</summary>
public sealed record UserQueryParameters
{
    /// <summary>Case-insensitive match against the email.</summary>
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>Replaces a user's single role.</summary>
public sealed record ChangeRoleRequest([Required] string Role);

/// <summary>Activates or deactivates a user account.</summary>
public sealed record SetActiveRequest(bool IsActive);
