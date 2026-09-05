using MoniPay.Sessions;
using MoniPay.Users;
using Xunit;

namespace MoniPay.Tests.Architecture;

/// <summary>
/// Asserts what the host is allowed to see. Entities, EF configurations and their constants
/// stay internal, so a later change cannot bind another project to them by accident. Wallet is
/// absent: it predates this rule and still exports its endpoint surface.
/// </summary>
public sealed class PublicSurfaceTests
{
    [Theory]
    [InlineData(typeof(SessionsModule))]
    [InlineData(typeof(UsersModule))]
    public void A_module_exposes_its_composition_entry_point_only(Type module)
    {
        IEnumerable<string?> exported = module.Assembly.GetExportedTypes()
            .Select(type => type.FullName);

        Assert.Equal([module.FullName], exported);
    }
}
