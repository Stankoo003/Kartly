using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kartly.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kartly.Infrastructure.Auth;

/// <summary>Builds signed JWTs that carry the user's identity and role.</summary>
public sealed class JwtTokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _settings = options.Value;

    public AuthResult CreateToken(ApplicationUser user, string role)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(_settings.ExpiryHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResult(encoded, user.Email!, role, expiresAt);
    }
}
