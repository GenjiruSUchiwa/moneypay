using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// The reads and locks every sign-up slice shares. Each mutating slice takes one lock, runs one
/// aggregate transition and commits. A row the workflow token named but the table no longer
/// holds — cleanup ran between authentication and the handler — is answered as an invalid
/// token, which is the one thing the client can act on.
/// </summary>
internal static class SignUpQueries
{
    // A constant, so no request value can reach the SQL text: the identifier travels as a parameter.
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

        /// <summary>
        /// Loads a sign-up under a row lock held until the transaction ends, so concurrent
        /// transitions of one sign-up serialize and each sees the state the previous one committed.
        /// </summary>
        public async Task<SignUp> LockSignUpAsync(SignUpId signUpId, CancellationToken cancellationToken)
        {
            SignUp? signUp = await database.SignUps
                .FromSqlRaw(LockForUpdateSql, signUpId.Value)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return signUp ?? throw new RefusalException(MoniPayErrorTypes.SignUpTokenInvalid);
        }

        /// <summary>
        /// The sign-up that still owns the phone, under the same row lock the resend and verify
        /// slices take, so a start racing either of them reads the state they committed instead of
        /// failing on the concurrency token. The phone lock alone does not give that: a plain read
        /// does not wait for a row locked <c>FOR UPDATE</c>.
        /// </summary>
        public Task<SignUp?> LockActiveSignUpAsync(LookupHash phoneHash, CancellationToken cancellationToken) =>
            database.SignUps
                .FromSqlRaw(LockActiveForUpdateSql, phoneHash.Value)
                .SingleOrDefaultAsync(cancellationToken);

        /// <summary>
        /// Serializes every start for one phone until the transaction ends. A phone may have no
        /// row yet, so the lock is an advisory one keyed by the phone hash rather than a row lock;
        /// it makes the start count exact and lets parallel starts reuse one row without a retry.
        /// </summary>
        public Task LockPhoneAsync(LookupHash phoneHash, CancellationToken cancellationToken)
        {
            // The hash is an HMAC, so its first eight bytes are as good a 64-bit key as any.
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
