using System.Reflection;
using Xunit;

namespace Rise.Architecture.Tests;

/// <summary>
/// The slice rule, enforced instead of assumed. Rise's hexagonal
/// layout rests on project references, and project references are
/// exactly what an assembly's manifest records — so these tests read
/// each assembly's referenced assemblies and assert the allowed set.
/// A new slice, or a new reference from an existing one, fails here
/// before it can quietly rot the layering. The shell (Rise.Wasm) is
/// deliberately untested: the composition root is the one place
/// allowed to reference everything.
/// </summary>
public class ArchitectureTests
{
    /// <summary>Every slice assembly. When a slice is added, add it here — the completeness test below fails until then.</summary>
    private static readonly string[] SliceAssemblies = new[]
    {
        "Rise.Slices.Backup",
        "Rise.Slices.Header",
        "Rise.Slices.Layout",
        "Rise.Slices.Links",
        "Rise.Slices.Notes",
        "Rise.Slices.Search",
        "Rise.Slices.Settings",
        "Rise.Slices.Todo",
        "Rise.Slices.Weather",
    };

    private static readonly string[] SliceContractAssemblies = new[]
    {
        "Rise.Slices.Settings.Abstractions",
    };

    /// <summary>Assembly names this assembly references that belong to Rise (framework packages are ignored).</summary>
    private static HashSet<string> RiseReferences(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("Rise.", StringComparison.Ordinal))
            .ToHashSet();

    private static void AssertAllowed(string assemblyName, HashSet<string> actual, params string[] allowed)
    {
        string[] forbidden = actual.Except(allowed).ToArray();
        Assert.True(forbidden.Length == 0,
            $"{assemblyName} references forbidden assemblies: {string.Join(", ", forbidden)}. Allowed: {string.Join(", ", allowed)}.");
    }

    /// <summary>
    /// The slice rule itself: a slice references only the core
    /// (Application, and Domain where it uses domain types directly)
    /// and the shared settings contract — never Infrastructure, never
    /// another slice, never the shell.
    /// </summary>
    [Theory]
    [InlineData("Rise.Slices.Backup")]
    [InlineData("Rise.Slices.Header")]
    [InlineData("Rise.Slices.Layout")]
    [InlineData("Rise.Slices.Links")]
    [InlineData("Rise.Slices.Notes")]
    [InlineData("Rise.Slices.Search")]
    [InlineData("Rise.Slices.Settings")]
    [InlineData("Rise.Slices.Todo")]
    [InlineData("Rise.Slices.Weather")]
    public void Slice_ReferencesOnlyCoreAndSettingsContract(string assemblyName) =>
        AssertAllowed(assemblyName, RiseReferences(assemblyName),
            "Rise.Application", "Rise.Domain", "Rise.Slices.Settings.Abstractions");

    /// <summary>Domain is pure: it references no other Rise assembly at all.</summary>
    [Fact]
    public void Domain_ReferencesNoRiseAssembly() =>
        AssertAllowed("Rise.Domain", RiseReferences("Rise.Domain"));

    /// <summary>Application sits on Domain and nothing else.</summary>
    [Fact]
    public void Application_ReferencesOnlyDomain() =>
        AssertAllowed("Rise.Application", RiseReferences("Rise.Application"), "Rise.Domain");

    /// <summary>Infrastructure adapts the core ports: Application, and Domain where it uses domain types directly. No slices, no shell.</summary>
    [Fact]
    public void Infrastructure_ReferencesOnlyCore() =>
        AssertAllowed("Rise.Infrastructure", RiseReferences("Rise.Infrastructure"),
            "Rise.Application", "Rise.Domain");

    /// <summary>The settings contract is dependency-free: it references no other Rise assembly.</summary>
    [Fact]
    public void SettingsContract_ReferencesNoRiseAssembly() =>
        AssertAllowed("Rise.Slices.Settings.Abstractions", RiseReferences("Rise.Slices.Settings.Abstractions"));

    /// <summary>
    /// A new slice must join the theory list above: every slice-shaped
    /// DLL in the output must be one this test knows. Otherwise a
    /// slice could ship with its references entirely unexamined.
    /// </summary>
    [Fact]
    public void SliceSet_IsComplete()
    {
        string directory = AppContext.BaseDirectory;
        var found = Directory.GetFiles(directory, "Rise.Slices.*.dll")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !name.EndsWith(".Tests", StringComparison.Ordinal))
            .ToHashSet();
        var expected = SliceAssemblies.Concat(SliceContractAssemblies).ToHashSet();
        Assert.True(found.SetEquals(expected),
            $"Slice assemblies in output do not match the tested set. Missing from output: {string.Join(", ", expected.Except(found))}. Untested in output: {string.Join(", ", found.Except(expected))}.");
    }
}
