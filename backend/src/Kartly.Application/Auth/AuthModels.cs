namespace Kartly.Application.Auth;

/// <summary>
/// Credentials for registration. Deliberately carries no role: public registration always
/// creates a Customer. Role assignment is admin-only (PUT /api/admin/users/{id}/role).
/// </summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>Credentials for login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Result of a successful authentication — the bearer token and its metadata.</summary>
public sealed record AuthResult(string Token, string Email, string Role, DateTimeOffset ExpiresAt);