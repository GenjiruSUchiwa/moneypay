using System.Reflection;
using MoniPay.Kernel;
using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Users;
using MoniPay.Wallet;
using Xunit;

namespace MoniPay.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] ModuleDependencies = ["MoniPay.Kernel", "MoniPay.Data"];

    [Theory]
    [InlineData(typeof(SessionsModule))]
    [InlineData(typeof(UsersModule))]
    [InlineData(typeof(WalletModule))]
    [InlineData(typeof(NotificationsModule))]
    public void A_module_references_nothing_beyond_the_kernel_and_the_shared_context(Type module)
    {
        IEnumerable<string> references = MoniPayReferencesOf(module.Assembly);

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
