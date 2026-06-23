namespace InsuranceCoreAPI.Errors;

/// <summary>
/// Base exception for all domain/application errors.
/// Maps to a specific HTTP status code via <see cref="GlobalExceptionHandler"/>.
/// </summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Mapped to HTTP 404 Not Found.</summary>
public sealed class NotFoundException(string message) : AppException(message);

/// <summary>Mapped to HTTP 409 Conflict (wrong status, overlap, business rule violation).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Mapped to HTTP 400 Bad Request (invalid input, missing required field).</summary>
public sealed class ValidationException(string message) : AppException(message);
