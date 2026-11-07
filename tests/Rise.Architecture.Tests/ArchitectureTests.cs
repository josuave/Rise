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
/// tested in the reverse direction only: as the composition root it
/// may reference everything, but nothing may reference it.
/// </summary>
public class ArchitectureTests
{
    /// <summary>Every slice assembly. When a slice is added, add it here — the completeness test below fails until then.</summary>
    private static readonly string[] SliceAssemblies = new[]
    {
        "Rise.Slices.Backup",
        "Rise.Slices.Game2048",
        "Rise.Slices.Header",
        "Rise.Slices.Layout",
        "Rise.Slices.Links",
        "Rise.Slices.Notes",
        "Rise.Slices.OmarchyThemes",
        "Rise.Slices.Search",
        "Rise.Slices.Settings",
        "Rise.Slices.Todo",
        "Rise.Slices.Weather",
    };

    private static readonly string[] SliceContractAssemblies = new[]
    {
        "Rise.Slices.Settings.Abstractions",
    };

    private static readonly string[] CoreAssemblies = new[]
    {
        "Rise.Domain",
        "Rise.Application",
        "Rise.Infrastructure",
    };

    /// <summary>Every Rise assembly except the shell — the set the shell-direction test loops over.</summary>
    private static readonly string[] NonShellAssemblies = SliceAssemblies
        .Concat(SliceContractAssemblies)
        .Concat(CoreAssemblies)
        .ToArray();

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
    [InlineData("Rise.Slices.Game2048")]
    [InlineData("Rise.Slices.Header")]
    [InlineData("Rise.Slices.Layout")]
    [InlineData("Rise.Slices.Links")]
    [InlineData("Rise.Slices.Notes")]
    [InlineData("Rise.Slices.OmarchyThemes")]
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
    /// The shell direction of the dependency rule: the composition
    /// root may reference everything, but nothing may reference the
    /// shell. A slice reaching into a shell type would point a
    /// dependency outward, and this is the test that catches it.
    /// </summary>
    [Fact]
    public void Nothing_ReferencesTheShell()
    {
        foreach (string assemblyName in NonShellAssemblies)
        {
            Assert.True(!RiseReferences(assemblyName).Contains("Rise.Wasm"),
                $"{assemblyName} references the shell (Rise.Wasm). Dependencies point inward; the shell is referenced by nothing.");
        }
    }

    /// <summary>
    /// The third-party guard for the core: Domain and Application
    /// reference no ASP.NET Core assembly, so no Blazor type can leak
    /// into the pure core through a transitive package reference.
    /// </summary>
    [Theory]
    [InlineData("Rise.Domain")]
    [InlineData("Rise.Application")]
    public void Core_ReferencesNoAspNetCoreAssembly(string assemblyName)
    {
        string[] forbidden = Assembly.Load(assemblyName).GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToArray();
        Assert.True(forbidden.Length == 0,
            $"{assemblyName} references ASP.NET Core assemblies: {string.Join(", ", forbidden)}. The core stays free of Blazor types.");
    }

    /// <summary>
    /// Slice colocation: every type in a slice assembly lives in that
    /// slice's own namespace. A type landing in another slice's (or a
    /// shared) namespace would blur the slice boundary the assembly
    /// references alone cannot see.
    /// </summary>
    [Theory]
    [InlineData("Rise.Slices.Backup")]
    [InlineData("Rise.Slices.Game2048")]
    [InlineData("Rise.Slices.Header")]
    [InlineData("Rise.Slices.Layout")]
    [InlineData("Rise.Slices.Links")]
    [InlineData("Rise.Slices.Notes")]
    [InlineData("Rise.Slices.OmarchyThemes")]
    [InlineData("Rise.Slices.Search")]
    [InlineData("Rise.Slices.Settings")]
    [InlineData("Rise.Slices.Settings.Abstractions")]
    [InlineData("Rise.Slices.Todo")]
    [InlineData("Rise.Slices.Weather")]
    public void Slice_TypesResideInTheirOwnNamespace(string assemblyName)
    {
        Type[] stray = Assembly.Load(assemblyName).GetTypes()
            .Where(type => type.Namespace != assemblyName && !IsCoverletTracker(type))
            .ToArray();
        Assert.True(stray.Length == 0,
            $"{assemblyName} has types outside its own namespace: {string.Join(", ", stray.Select(type => type.FullName))}.");
    }

    /// <summary>
    /// Coverlet's coverage collection injects one tracker type per
    /// instrumented assembly (Coverlet.Core.Instrumentation.Tracker.
    /// &lt;AssemblyName&gt;_&lt;guid&gt;). It is not a type the project
    /// wrote, so the colocation rule ignores it — otherwise this test
    /// fails only when coverage runs, which is exactly when CI runs it.
    /// </summary>
    private static bool IsCoverletTracker(Type type) =>
        type.Namespace?.StartsWith("Coverlet.Core.Instrumentation", StringComparison.Ordinal) == true;

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
