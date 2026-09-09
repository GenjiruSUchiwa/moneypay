using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Users.Persistence;
using MoniPay.Users.Security;

namespace MoniPay.Users.Features.Registration;

public sealed class PhoneRegistrationLookup
{
    private readonly MoniPayDbContext database;
    private readonly UserLookupDigest lookupDigest;

    internal PhoneRegistrationLookup(MoniPayDbContext database, UserLookupDigest lookupDigest)
    {
        this.database = database;
        this.lookupDigest = lookupDigest;
    }

    public async Task<UserId?> FindUserIdAsync(PhoneNumber phone, CancellationToken cancellationToken)
    {
        return await database.Users
            .Where(user => user.Phone.Hash.Equals(lookupDigest.Compute(phone.Value)))
            .Select(user => (UserId?)user.Id)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
