using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Users.Domain;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;

namespace MoniPay.Users.Features.CurrentUser;

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
