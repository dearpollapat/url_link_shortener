namespace UrlShortener.Api.Core;

/// <summary>Category of a business error, mapped to an HTTP status at the edge.</summary>
public enum ErrorType
{
    Validation,
    Conflict,
    NotFound
}

/// <summary>An expected, user-facing failure — carried by <see cref="Result"/> instead of thrown.</summary>
public sealed record Error(ErrorType Type, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
}

/// <summary>Outcome of an operation with no return value.</summary>
public readonly struct Result
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that yields a <typeparamref name="T"/> on success.</summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    // Let handlers `return value;` or `return Error.Validation(...);` directly.
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
