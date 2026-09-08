namespace FoodTruckApi.Domain.Common;

/// <summary>
/// Broad category of a failure, used at the API boundary to pick an HTTP status code
/// without the inner layers needing to know about HTTP.
/// </summary>
public enum ErrorType
{
    /// <summary>The caller sent something invalid; maps to 400.</summary>
    Validation,

    /// <summary>Something went wrong on our side; maps to 500.</summary>
    Unexpected,
}

/// <summary>
/// A single, machine-readable failure. <see cref="Code"/> is a stable identifier
/// (e.g. <c>latitude</c>); <see cref="Message"/> is human-readable.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Placeholder carried by successful results, which have no error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
