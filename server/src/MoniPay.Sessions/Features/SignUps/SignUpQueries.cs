using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;

namespace MoniPay.Sessions.Features.SignUps;

internal static class SignUpQueries
{
    private const string LockForUpdateSql =
        $"SELECT * FROM {SessionsSchema.SignUpsTable} WHERE id = {{0}} FOR UPDATE";

    private static readonly string LockActiveForUpdateSql =
        $"SELECT * FROM {SessionsSchema.SignUpsTable} WHERE phone_lookup_hash = {{0}} AND {SessionsSchema.ActiveStatusFilter} FOR UPDATE";

    extension(MoniPayDbContext database)
    {
        public async Task<SignUp> ReadSignUpAsync(SignUpId signUpId, CancellationToken cancellationToken)
        {
            SignUp? signUp = await database.SignUps
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == signUpId, cancellationToken)
                .ConfigureAwait(false);

            return signUp ?? throw new RefusalException(MoniPayErrorTypes.SignUpTokenInvalid);
        }

        public async Task<SignUp> LockSignUpAsync(SignUpId signUpId, CancellationToken cancellationToken)
        {
            SignUp? signUp = await database.SignUps
                .FromSqlRaw(LockForUpdateSql, signUpId.Value)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return signUp ?? throw new RefusalException(MoniPayErrorTypes.SignUpTokenInvalid);
        }

        public Task<SignUp?> LockActiveSignUpAsync(LookupHash phoneHash, CancellationToken cancellationToken) =>
            database.SignUps
                .FromSqlRaw(LockActiveForUpdateSql, phoneHash.Value)
                .SingleOrDefaultAsync(cancellationToken);

        public Task LockPhoneAsync(LookupHash phoneHash, CancellationToken cancellationToken)
        {
            long key = BinaryPrimitives.ReadInt64LittleEndian(phoneHash.Value);
            return database.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
        }

        public async Task CommitAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
        {
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
