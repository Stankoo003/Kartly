using Kartly.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Registers a user (Customer by default) and returns a JWT.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.RegisterAsync(request, ct));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Validates credentials and issues a JWT carrying the user's role.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.LoginAsync(request, ct));
        }
        catch (AuthException)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }
}
