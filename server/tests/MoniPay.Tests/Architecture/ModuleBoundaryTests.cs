using System.Reflection;
using MoniPay.Kernel;
using MoniPay.Users;
using Xunit;

namespace MoniPay.Tests.Architecture;

/// <summary>
/// Asserts the layering the projects claim: a module talks to the kernel and to the shared
/// context, never to a sibling module, and the kernel talks to nobody.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] ModuleDependencies = ["MoniPay.Kernel", "MoniPay.Data"];

    [Fact]
    public void The_users_module_references_nothing_beyond_the_kernel_and_the_shared_context()
    {
        IEnumerable<string> references = MoniPayReferencesOf(typeof(UsersModule).Assembly);

        Assert.Empty(references.Except(ModuleDependencies));
    }

    [Fact]
    public void The_kernel_references_no_other_project()
    {
        Assert.Empty(MoniPayReferencesOf(typeof(UserId).Assembly));
    }

    private static IEnumerable<string> MoniPayReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(name => name.StartsWith("MoniPay.", StringComparison.Ordinal));
}
