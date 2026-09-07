namespace FoodTruckApi.Domain.Common;

/// <summary>
/// Outcome of an operation that is expected to fail in normal use (invalid input, a
/// missing record, …). Failures are values, not exceptions. Exceptions are reserved for
/// programmer errors and unrecoverable startup conditions.
/// </summary>
public class Result
{
    private protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        switch (isSuccess)
        {
            case true when errors.Count > 0:
                throw new InvalidOperationException("A successful result cannot carry errors.");
            case false when errors.Count == 0:
                throw new InvalidOperationException("A failed result must carry at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>All failures. Empty on success. Validation collects every problem, not just the first.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The first failure, or <see cref="Error.None"/> on success.</summary>
    public Error Error => Errors.Count > 0 ? Errors[0] : Error.None;

    public static Result Success() => new(true, Array.Empty<Error>());

    public static Result Failure(Error error) => new(false, new[] { error });

    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(value, true, Array.Empty<Error>());

    public static Result<T> Failure<T>(Error error) => new(default!, false, new[] { error });

    public static Result<T> Failure<T>(IReadOnlyList<Error> errors) => new(default!, false, errors);
}

/// <summary>A <see cref="Result"/> that also carries a value when successful.</summary>
public sealed class Result<T> : Result
{
    private readonly T _value;

    internal Result(T value, bool isSuccess, IReadOnlyList<Error> errors)
        : base(isSuccess, errors) => _value = value;

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static implicit operator Result<T>(T value) => Success(value);
}
