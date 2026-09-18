using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;

namespace MoniPay.Users.Features.Contact;

public sealed class UserContactLookup
{
    private readonly MoniPayDbContext database;
    private readonly UserPersonalDataProtector personalData;

    internal UserContactLookup(MoniPayDbContext database, UserPersonalDataProtector personalData)
    {
        this.database = database;
        this.personalData = personalData;
    }

    public async Task<UserContact?> FindAsync(UserId userId, CancellationToken cancellationToken)
    {
        var row = await database.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                Phone = user.Phone.Ciphertext,
                Email = user.Email.Ciphertext,
                user.Locale,
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        return new UserContact(
            new PhoneNumber(personalData.Unprotect(row.Phone)),
            new EmailAddress(personalData.Unprotect(row.Email)),
            row.Locale);
    }
}
