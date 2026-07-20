namespace Kartly.Application.Auth;

/// <summary>
/// Authentication contract. Defined in the Application layer; implemented in
/// Infrastructure using ASP.NET Identity for user storage / password hashing
/// and JWT for token issuance — the domain stays unaware of those details.
/// </summary>
public interface IAuthService
{
    /// <summary>Creates a user and returns a signed token. Throws <see cref="AuthException"/> on failure.</summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Validates credentials and returns a signed token. Throws <see cref="AuthException"/> on failure.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

/// <summary>Raised when registration or login fails for a client-correctable reason.</summary>
public sealed class AuthException(string message) : Exception(message);