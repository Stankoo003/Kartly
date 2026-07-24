namespace Kartly.Application.Users;

/// <summary>Raised when a user cannot be found. Mapped to HTTP 404 by the controller.</summary>
public sealed class UserNotFoundException(string id)
    : Exception($"User '{id}' was not found.");

/// <summary>
/// Raised when an admin operation is refused by a business rule — an invalid role,
/// self-demotion / self-deactivation, or removing the last remaining admin.
/// Mapped to HTTP 409 by the controller.
/// </summary>
public sealed class UserAdminException(string message) : Exception(message);
