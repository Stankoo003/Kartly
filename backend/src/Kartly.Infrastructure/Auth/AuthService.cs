using Kartly.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// ASP.NET Identity–backed implementation of <see cref="IAuthService"/>: it owns
/// user creation, password verification (hashing handled by Identity) and hands
/// the resulting principal to <see cref="JwtTokenService"/> for token issuance.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenService tokenService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new AuthException("A user with this email already exists.");

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new AuthException(string.Join(" ", created.Errors.Select(e => e.Description)));

        // Public registration always creates a Customer; the role is never taken from the request.
        await userManager.AddToRoleAsync(user, Roles.Customer);
        return tokenService.CreateToken(user, Roles.Customer);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new AuthException("Invalid email or password.");

        if (!user.IsActive)
            throw new AuthException("This account has been deactivated.");

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Customer;
        return tokenService.CreateToken(user, role);
    }
}
