using MoniPay.Users;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Architecture;

/// <summary>
/// Asserts what the host is allowed to see. Entities, EF configurations and their constants
/// stay internal, so a later change cannot bind another project to them by accident.
/// </summary>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void The_users_module_exposes_its_registration_slice_and_composition_entry_point_only()
    {
        IEnumerable<string?> exported = typeof(UsersModule).Assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Order();

        string?[] expected =
        [
            typeof(RegisteredUser).FullName,
            typeof(RegisterUserCommand).FullName,
            typeof(RegisterUserHandler).FullName,
            typeof(UsersModule).FullName,
        ];
        Assert.Equal(expected.Order(), exported);
    }
}
