namespace Rise.Domain;

/// <summary>A pure result: either a value, or a human-readable error. No exceptions, no I/O.</summary>
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
