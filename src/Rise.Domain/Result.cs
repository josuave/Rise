namespace Rise.Domain;

/// <summary>The success case of a <see cref="Result{T}"/>: the produced value.</summary>
public sealed record Ok<T>(T Value);

/// <summary>The failure case of a <see cref="Result{T}"/>: a human-readable reason.</summary>
public sealed record Error(string Message);

/// <summary>
/// A pure result: either a value, or a human-readable error. No exceptions, no I/O.
/// A C# 15 union, so callers switch on it exhaustively: a new case warns (CS8509) at
/// every switch that does not handle it yet. The union lowers to a struct; a default
/// value holds null contents, so switches add an explicit <c>null</c> arm rather
/// than a <c>_</c> arm (which would swallow a future case). All current type
/// arguments are reference types, so the <c>Ok</c> case is a reference held in the
/// union's object field with no boxing; a value-type argument would box.
/// </summary>
public union Result<T>(Ok<T>, Error) : IEquatable<Result<T>>
{
    public static Result<T> Success(T value) => new Ok<T>(value);

    public static Result<T> Failure(string error) => new Error(error);

    // The union lowers to a plain struct, so equality is hand-written
    // (CA1815, CA1066): two results are equal when their cases hold equal values.
    public bool Equals(Result<T> other) => Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}
