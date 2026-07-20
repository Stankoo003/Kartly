using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// EF Core context backing ASP.NET Identity (users, roles, claims) on PostgreSQL.
/// </summary>
public sealed class KartlyDbContext(DbContextOptions<KartlyDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options);
