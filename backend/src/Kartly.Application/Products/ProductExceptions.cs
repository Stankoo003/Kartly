namespace Kartly.Application.Products;

/// <summary>Raised when a product cannot be found. Mapped to HTTP 404 by the controller.</summary>
public sealed class ProductNotFoundException(Guid id)
    : Exception($"Product '{id}' was not found.");

/// <summary>
/// Raised when a create/update would violate the unique slug or sku constraint.
/// Mapped to HTTP 409 by the controller.
/// </summary>
public sealed class ProductConflictException(string message) : Exception(message);
