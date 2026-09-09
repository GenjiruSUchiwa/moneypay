using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;

namespace MoniPay.Users.Features.CurrentUser;

/// <summary>
/// Reads the user the caller's ticket names. The active-session policy has already proven the
/// session belongs to this user, so the read scopes to the user identifier alone: the users
/// table owns no session column to scope to. (The session read scopes to both identifiers
/// because the sessions table owns both.) A ticket whose user is gone names nothing to answer
/// for, so it is refused as an invalid credential — a 401 the client can act on by dropping the
/// credential — never a 404 that would confirm the identifier, and never a 500. Contact
/// decryption stays inside Users.
/// </summary>
internal sealed class GetCurrentUserHandler(MoniPayDbContext database, UserPersonalDataProtector personalData)
{
    public async Task<CurrentUserView> HandleAsync(UserId userId, CancellationToken cancellationToken)
    {
        User user = await database.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new RefusalException(MoniPayErrorTypes.SessionInvalid);

        return new CurrentUserView(
            user.Id,
            personalData.Unprotect(user.FirstName),
            personalData.Unprotect(user.LastName),
            personalData.Unprotect(user.Phone.Ciphertext),
            personalData.Unprotect(user.Email.Ciphertext),
            user.Locale,
            user.CreatedAt);
    }
}
