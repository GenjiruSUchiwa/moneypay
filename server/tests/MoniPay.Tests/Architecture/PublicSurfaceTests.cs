using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Sessions.Ports;
using MoniPay.Sessions.Providers;
using MoniPay.Sessions.Security;
using MoniPay.Users;
using MoniPay.Users.Features.Contact;
using MoniPay.Users.Features.Registration;
using MoniPay.Users.Providers;
using Xunit;

namespace MoniPay.Tests.Architecture;

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
                typeof(ISecurityAlertSender),
                typeof(SecurityAlert),
                typeof(SecurityAlertKind),
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
                typeof(UserMessages),
                typeof(RegisterUserHandler),
                typeof(RegisterUserCommand),
                typeof(RegisteredUser),
                typeof(PhoneRegistrationLookup),
                typeof(IWelcomeMessageSender),
                typeof(WelcomeMessage),
                typeof(UserContactLookup),
                typeof(UserContact),
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
