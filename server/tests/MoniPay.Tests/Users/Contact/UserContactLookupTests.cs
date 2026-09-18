using MoniPay.Kernel;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.Contact;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Users.Contact;

public sealed class UserContactLookupTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task It_returns_the_decrypted_contact_and_the_stored_locale()
    {
        PhoneNumber phone = new(TestPhones.Next());
        EmailAddress email = new($"lookup{Guid.NewGuid():N}@example.com");

        try
        {
            RegisteredUser user = await Api.RegisterUserAsync(phone, email.Value);

            UserContact? found = await Api.InScopeAsync<UserContactLookup, UserContact?>(
                (lookup, cancellationToken) => lookup.FindAsync(user.Id, cancellationToken));

            UserContact contact = Assert.IsType<UserContact>(found);
            Assert.Equal(phone, contact.Phone);
            Assert.Equal(email, contact.Email);
            Assert.Equal(Locale.FrenchCameroon, contact.Locale);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task An_unknown_user_has_no_contact()
    {
        UserContact? found = await Api.InScopeAsync<UserContactLookup, UserContact?>(
            (lookup, cancellationToken) => lookup.FindAsync(UserId.New(), cancellationToken));

        Assert.Null(found);
    }
}
