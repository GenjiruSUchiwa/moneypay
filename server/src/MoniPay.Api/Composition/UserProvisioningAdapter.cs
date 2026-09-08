using MoniPay.Kernel;
using MoniPay.Sessions.Features.SignUps.Complete;
using MoniPay.Users.Features.Registration;

namespace MoniPay.Api.Composition;

/// <summary>
/// Maps the Sessions provisioning port onto the Users registration slice; the only rule here is
/// the mapping — same scoped context, joins the caller's transaction.
/// </summary>
internal sealed class UserProvisioningAdapter(RegisterUserHandler users) : IUserProvisioning
{
    public async Task<UserId> ProvisionAsync(ProvisionUserRequest request, CancellationToken cancellationToken)
    {
        RegisteredUser registered = await users.HandleAsync(
            new RegisterUserCommand(
                request.SignUpId,
                request.UserId,
                request.Phone,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Locale,
                request.TermsVersion,
                request.PrivacyVersion,
                request.AcceptedAt),
            cancellationToken).ConfigureAwait(false);
        return registered.Id;
    }
}
