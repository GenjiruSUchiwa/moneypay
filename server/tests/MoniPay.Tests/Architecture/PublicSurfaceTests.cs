using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Ports;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;
using MoniPay.Users;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Architecture;

/// <summary>
/// Asserts what the host is allowed to see: the composition entry point, and the ports the host
/// implements for the module. Entities, handlers, EF configurations and their constants stay
/// internal, so a later change cannot bind another project to them by accident. The deliberate
/// extra surface is the vocabulary the host composes: the rate-limit policy names, the scheme
/// names. Wallet is absent: it predates
/// this rule and still exports its endpoint surface.
/// </summary>
public sealed class PublicSurfaceTests
{
    public static TheoryData<Type, Type[]> Modules => new()
    {
        {
            typeof(SessionsModule),
            [
                typeof(SessionsModule),
                typeof(SessionMessages),
                typeof(SessionMessageKeys),
                typeof(IVerificationCodeSender),
                typeof(VerificationCodeMessage),
                typeof(CodeDeliveryState),
                typeof(IRegisteredPhoneLookup),
                typeof(SessionsSchemes),
                typeof(SignUpRateLimitPolicies),
                typeof(SessionRateLimitPolicies),
                typeof(IUserProvisioning),
                typeof(ProvisionUserRequest),
            ]
        },
        {
            typeof(UsersModule),
            [
                typeof(UsersModule),
                typeof(RegisterUserHandler),
                typeof(RegisterUserCommand),
                typeof(RegisteredUser),
                typeof(PhoneRegistrationLookup),
            ]
        },
        {
            typeof(NotificationsModule),
            [
                typeof(NotificationsModule),
                typeof(NotificationOutbox),
                typeof(OutboundMessage),
                typeof(NotificationChannel),
                typeof(NotificationStatus),
            ]
        },
    };

    [Theory]
    [MemberData(nameof(Modules))]
    public void A_module_exposes_its_composition_entry_point_and_its_ports_only(Type module, Type[] expected)
    {
        IEnumerable<string?> exported = module.Assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .Order();

        Assert.Equal(expected.Select(type => type.FullName).Order(), exported);
    }
}
