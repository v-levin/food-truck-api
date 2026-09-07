namespace FoodTruckApi.Domain.Common;

/// <summary>
/// Broad category of a failure, used at the API boundary to pick an HTTP status code
/// without the inner layers needing to know about HTTP.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unexpected,
}

/// <summary>
/// A single, machine-readable failure. <see cref="Code"/> is a stable dotted identifier
/// (e.g. <c>coordinate.latitude</c>); <see cref="Message"/> is human-readable.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Placeholder carried by successful results, which have no error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
