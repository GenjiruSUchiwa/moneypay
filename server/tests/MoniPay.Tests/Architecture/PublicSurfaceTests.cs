using MoniPay.Users;
using Xunit;

namespace MoniPay.Tests.Architecture;

/// <summary>
/// Asserts what the host is allowed to see. Entities, EF configurations and their constants
/// stay internal, so a later change cannot bind another project to them by accident.
/// </summary>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void The_users_module_exposes_its_composition_entry_point_only()
    {
        IEnumerable<string?> exported = typeof(UsersModule).Assembly.GetExportedTypes()
            .Select(type => type.FullName);

        Assert.Equal([typeof(UsersModule).FullName], exported);
    }
}
