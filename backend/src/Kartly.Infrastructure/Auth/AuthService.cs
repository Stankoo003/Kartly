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
        var role = NormalizeRole(request.Role);

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new AuthException("A user with this email already exists.");

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new AuthException(string.Join(" ", created.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
        return tokenService.CreateToken(user, role);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new AuthException("Invalid email or password.");

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Customer;
        return tokenService.CreateToken(user, role);
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Roles.Customer;

        var match = Roles.All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new AuthException($"Unknown role '{role}'.");
    }
}
