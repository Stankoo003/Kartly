namespace Kartly.Application.Auth;

/// <summary>Credentials for registration. Role defaults to Customer if omitted.</summary>
public sealed record RegisterRequest(string Email, string Password, string? Role = null);

/// <summary>Credentials for login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Result of a successful authentication — the bearer token and its metadata.</summary>
public sealed record AuthResult(string Token, string Email, string Role, DateTimeOffset ExpiresAt);