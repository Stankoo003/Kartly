using Microsoft.AspNetCore.Identity;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// The persisted user. Extends the ASP.NET Identity user so we inherit its
/// storage schema and secure password hashing (PBKDF2 + per-user salt).
/// </summary>
public sealed class ApplicationUser : IdentityUser;
