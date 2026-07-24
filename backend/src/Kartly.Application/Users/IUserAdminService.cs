using Kartly.Application.Products; // reuse the generic PagedResult<T>

namespace Kartly.Application.Users;

/// <summary>Admin-only user administration: listing, role changes and activation.</summary>
public interface IUserAdminService
{
    Task<PagedResult<UserResponse>> GetUsersAsync(UserQueryParameters query, CancellationToken ct = default);

    Task<UserResponse> GetUserByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Replaces the user's role. <paramref name="actingUserId"/> is the calling admin (for self-guards).</summary>
    Task<UserResponse> ChangeRoleAsync(string id, string role, string actingUserId, CancellationToken ct = default);

    /// <summary>Activates/deactivates the user. <paramref name="actingUserId"/> is the calling admin (for self-guards).</summary>
    Task<UserResponse> SetActiveAsync(string id, bool isActive, string actingUserId, CancellationToken ct = default);
}
