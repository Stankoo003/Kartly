using Kartly.Application.Auth;
using Kartly.Application.Products; // PagedResult<T>
using Kartly.Application.Users;
using Kartly.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Users;

/// <summary>
/// ASP.NET Identity–backed user administration. Enforces the single-role invariant and the
/// "don't lock everyone out" guards (no self-demotion / self-deactivation, never remove the
/// last active admin).
/// </summary>
public sealed class UserAdminService(
    UserManager<ApplicationUser> userManager,
    KartlyDbContext context) : IUserAdminService
{
    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        UserQueryParameters query, CancellationToken ct = default)
    {
        var users = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(u => u.Email != null && EF.Functions.ILike(u.Email, term));
        }

        // Project each user's single role via a correlated subquery (one row per user,
        // regardless of how many role rows exist) so pagination counts stay correct.
        var projected = users.Select(u => new
        {
            User = u,
            RoleName = (from ur in context.UserRoles
                        join r in context.Roles on ur.RoleId equals r.Id
                        where ur.UserId == u.Id
                        select r.Name).FirstOrDefault(),
        }).OrderBy(x => x.User.Email);

        var total = await projected.CountAsync(ct);
        var items = await projected
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var responses = items
            .Select(x => new UserResponse(x.User.Id, x.User.Email!, x.RoleName ?? Roles.Customer, x.User.IsActive))
            .ToList();

        return new PagedResult<UserResponse>(responses, query.Page, query.PageSize, total);
    }

    public async Task<UserResponse> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new UserNotFoundException(id);
        return Map(user, await GetRoleAsync(user));
    }

    public async Task<UserResponse> ChangeRoleAsync(
        string id, string role, string actingUserId, CancellationToken ct = default)
    {
        var normalized = Roles.All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase))
            ?? throw new UserAdminException($"Unknown role '{role}'. Allowed roles: {string.Join(", ", Roles.All)}.");

        var user = await userManager.FindByIdAsync(id) ?? throw new UserNotFoundException(id);
        var currentRole = await GetRoleAsync(user);

        if (string.Equals(currentRole, normalized, StringComparison.Ordinal))
            return Map(user, normalized); // no change

        var leavingAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.Ordinal);
        if (leavingAdmin)
        {
            if (string.Equals(id, actingUserId, StringComparison.Ordinal))
                throw new UserAdminException("You cannot change your own admin role.");
            // Only an *active* admin counts toward availability, so demoting one is blocked
            // only when it would leave no active admins behind.
            if (user.IsActive && await CountActiveAdminsAsync(ct) <= 1)
                throw new UserAdminException("Cannot remove the last remaining admin.");
        }

        var existing = await userManager.GetRolesAsync(user);
        if (existing.Count > 0)
            await userManager.RemoveFromRolesAsync(user, existing);
        await userManager.AddToRoleAsync(user, normalized);

        return Map(user, normalized);
    }

    public async Task<UserResponse> SetActiveAsync(
        string id, bool isActive, string actingUserId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new UserNotFoundException(id);

        if (!isActive && user.IsActive)
        {
            if (string.Equals(id, actingUserId, StringComparison.Ordinal))
                throw new UserAdminException("You cannot deactivate your own account.");
            if (await userManager.IsInRoleAsync(user, Roles.Admin) && await CountActiveAdminsAsync(ct) <= 1)
                throw new UserAdminException("Cannot deactivate the last remaining admin.");
        }

        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            await userManager.UpdateAsync(user);
        }

        return Map(user, await GetRoleAsync(user));
    }

    private async Task<string> GetRoleAsync(ApplicationUser user)
        => (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Customer;

    /// <summary>Number of active users currently in the Admin role.</summary>
    private async Task<int> CountActiveAdminsAsync(CancellationToken ct)
    {
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        return admins.Count(u => u.IsActive);
    }

    private static UserResponse Map(ApplicationUser u, string role) => new(u.Id, u.Email!, role, u.IsActive);
}
