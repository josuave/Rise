using Rise.Domain;

namespace Rise.Core.Tests;

/// <summary>
/// Unwraps <see cref="Result{T}"/> values in tests. Each unwrap is an
/// exhaustive switch over the union, so a future case fails loudly here
/// instead of a test silently passing on a wrong shape.
/// </summary>
internal static class ResultAssertions
{
    internal static T RequireOk<T>(Result<T> result) => result switch
    {
        Ok<T> ok => ok.Value,
        Error error => throw new InvalidOperationException($"Expected a success but got error: {error.Message}"),
        null => throw new InvalidOperationException("Expected a success but got a default result."),
    };

    internal static string RequireError<T>(Result<T> result) => result switch
    {
        Error error => error.Message,
        Ok<T> => throw new InvalidOperationException("Expected an error but got a success."),
        null => throw new InvalidOperationException("Expected an error but got a default result."),
    };
}
