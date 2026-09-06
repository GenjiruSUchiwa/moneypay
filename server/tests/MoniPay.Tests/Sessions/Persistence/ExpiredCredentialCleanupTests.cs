using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoniPay.Kernel;
using MoniPay.Persistence;
using MoniPay.Sessions;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Tests.Support;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Sessions.Persistence;

/// <summary>
/// The credential store's invariants against PostgreSQL: the partial index, the concurrency
/// token, replay detection and every rule the cleanup sweep must respect.
/// </summary>
public sealed class ExpiredCredentialCleanupTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task A_second_unconsumed_refresh_token_per_session_is_refused_by_the_partial_index()
    {
        DateTimeOffset now = Api.Time.GetUtcNow();
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), now);
        RefreshToken first = RefreshToken.Issue(session.Id, Digest(), now, TimeSpan.FromDays(1));
        database.Sessions.Add(session);
        database.RefreshTokens.Add(first);
        await database.SaveChangesAsync(Cancellation);

        first.Consume(Guid.CreateVersion7(), now);
        database.RefreshTokens.Add(RefreshToken.Issue(session.Id, Digest(), now, TimeSpan.FromDays(1)));
        await database.SaveChangesAsync(Cancellation);

        database.RefreshTokens.Add(RefreshToken.Issue(session.Id, Digest(), now, TimeSpan.FromDays(1)));
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync(Cancellation));

        PostgresException violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(SessionsSchema.RefreshTokenActivePerSessionUnique, violation.ConstraintName);
    }

    [Fact]
    public async Task A_stale_session_update_throws_a_concurrency_exception()
    {
        DateTimeOffset now = Api.Time.GetUtcNow();
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), now);
        await using (AsyncServiceScope seed = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = seed.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            database.Sessions.Add(session);
            await database.SaveChangesAsync(Cancellation);
        }

        await using AsyncServiceScope first = Api.Services.CreateAsyncScope();
        await using AsyncServiceScope second = Api.Services.CreateAsyncScope();
        MoniPayDbContext firstDatabase = first.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        MoniPayDbContext secondDatabase = second.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session firstCopy = await firstDatabase.Sessions.SingleAsync(row => row.Id == session.Id, Cancellation);
        Session secondCopy = await secondDatabase.Sessions.SingleAsync(row => row.Id == session.Id, Cancellation);

        firstCopy.Touch(now + TimeSpan.FromMinutes(1));
        await firstDatabase.SaveChangesAsync(Cancellation);
        secondCopy.Revoke(SessionRevokeReason.UserRequest, now + TimeSpan.FromMinutes(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondDatabase.SaveChangesAsync(Cancellation));
    }

    [Fact]
    public async Task A_consumed_token_keeps_its_digest_and_used_at_so_a_replay_is_found()
    {
        DateTimeOffset now = Api.Time.GetUtcNow();
        byte[] digest = Digest();
        Session session = Session.Create(UserId.New(), Guid.CreateVersion7(), now);
        RefreshToken token = RefreshToken.Issue(session.Id, digest, now, TimeSpan.FromDays(1));
        Guid replacementId = Guid.CreateVersion7();
        await using (AsyncServiceScope seed = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = seed.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            database.Sessions.Add(session);
            database.RefreshTokens.Add(token);
            await database.SaveChangesAsync(Cancellation);
            token.Consume(replacementId, now);
            await database.SaveChangesAsync(Cancellation);
        }

        await using AsyncServiceScope check = Api.Services.CreateAsyncScope();
        MoniPayDbContext reader = check.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        RefreshToken replayed = await reader.RefreshTokens.AsNoTracking().SingleAsync(row => row.TokenDigest == digest, Cancellation);

        Assert.True(replayed.IsConsumed);
        Assert.Equal(now, replayed.UsedAt);
        Assert.Equal(replacementId, replayed.ReplacedById);
    }

    [Fact]
    public async Task Cleanup_deletes_stale_credentials_in_bounded_batches_and_keeps_what_is_still_live()
    {
        SessionsOptions options = Api.Services.GetRequiredService<IOptions<SessionsOptions>>().Value;
        int batchSize = options.Cleanup.BatchSize;
        DateTimeOffset now = Api.Time.GetUtcNow();
        DateTimeOffset stale = now - options.StartWindow - options.SignUpLifetime - TimeSpan.FromDays(1);
        TimeSpan longLife = TimeSpan.FromDays(365);

        Session activeSession = Session.Create(UserId.New(), Guid.CreateVersion7(), stale);
        Session revokedSession = Session.Create(UserId.New(), Guid.CreateVersion7(), stale);
        revokedSession.Revoke(SessionRevokeReason.UserRequest, stale);
        List<RefreshToken> staleTokens = [];
        RefreshToken previous = RefreshToken.Issue(activeSession.Id, Digest(), stale, longLife);
        staleTokens.Add(previous);
        for (int i = 1; i < batchSize + 2; i++)
        {
            RefreshToken next = RefreshToken.Issue(activeSession.Id, Digest(), stale + TimeSpan.FromMinutes(i), longLife);
            previous.Consume(next.Id, stale + TimeSpan.FromMinutes(i));
            staleTokens.Add(next);
            previous = next;
        }

        // The newest token of the active session is consumed and old, yet must survive.
        RefreshToken newestOfActive = previous;
        newestOfActive.Consume(Guid.CreateVersion7(), stale + TimeSpan.FromDays(1));
        RefreshToken newestOfRevoked = RefreshToken.Issue(revokedSession.Id, Digest(), stale, longLife);
        newestOfRevoked.Consume(Guid.CreateVersion7(), stale);
        RefreshToken recentUnused = RefreshToken.Issue(Session.Create(UserId.New(), Guid.CreateVersion7(), now).Id, Digest(), now, longLife);

        SignUp staleOpen = SignUpAt(stale, options);
        SignUp staleCompleted = SignUpAt(stale, options);
        Assert.Equal(PhoneVerificationOutcome.Verified, staleCompleted.VerifyPhone(CodeDigest, stale, options));
        staleCompleted.Complete(UserId.New(), Guid.CreateVersion7(), stale);
        SignUp recentOpen = SignUpAt(now, options);

        await using (AsyncServiceScope seed = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = seed.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            database.Sessions.AddRange(activeSession, revokedSession);
            database.RefreshTokens.AddRange(staleTokens);
            database.RefreshTokens.AddRange(newestOfRevoked, recentUnused);
            database.SignUps.AddRange(staleOpen, staleCompleted, recentOpen);
            await database.SaveChangesAsync(Cancellation);
        }

        int expected = batchSize + 1 + 1 + 1; // stale non-newest tokens, the revoked session's token, the stale open sign-up
        int firstBatch;
        await using (AsyncServiceScope batch = Api.Services.CreateAsyncScope())
        {
            MoniPayDbContext database = batch.ServiceProvider.GetRequiredService<MoniPayDbContext>();
            ExpiredCredentialCleanupService cleanup = Api.Services.GetRequiredService<ExpiredCredentialCleanupService>();
            firstBatch = await cleanup.DeleteBatchAsync(database, now - options.StartWindow, Cancellation);
        }

        // Rows other tests left behind may be swept too, so the cycle count is a floor, not an equality.
        Assert.InRange(firstBatch, 1, 2 * batchSize);
        Assert.InRange(await Api.RunCleanupCycleAsync(Cancellation), expected - firstBatch, int.MaxValue);
        Assert.Equal(0, await Api.RunCleanupCycleAsync(Cancellation));

        await using AsyncServiceScope check = Api.Services.CreateAsyncScope();
        MoniPayDbContext reader = check.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Guid[] sessionIds = [activeSession.Id, revokedSession.Id];
        Assert.Equal(2, await reader.Sessions.CountAsync(row => sessionIds.Contains(row.Id), Cancellation));
        Guid[] remainingTokens = await reader.RefreshTokens
            .Where(row => row.SessionId == activeSession.Id || row.SessionId == revokedSession.Id || row.Id == recentUnused.Id)
            .Select(row => row.Id)
            .ToArrayAsync(Cancellation);
        Assert.Equal(new[] { newestOfActive.Id, recentUnused.Id }.Order(), remainingTokens.Order());
        SignUpId[] signUpIds = [staleOpen.Id, staleCompleted.Id, recentOpen.Id];
        SignUpId[] remainingSignUps = await reader.SignUps
            .Where(row => signUpIds.Contains(row.Id))
            .Select(row => row.Id)
            .ToArrayAsync(Cancellation);
        Assert.Equal(new[] { staleCompleted.Id, recentOpen.Id }.OrderBy(id => id.Value), remainingSignUps.OrderBy(id => id.Value));
    }

    private static readonly byte[] CodeDigest = [0xAA, 0xBB];

    private static byte[] Digest() => SHA256.HashData(Guid.CreateVersion7().ToByteArray());

    private static SignUp SignUpAt(DateTimeOffset startedAt, SessionsOptions options) =>
        SignUp.Start(
            SignUpId.New(),
            new Ciphertext($"cipher-{Guid.CreateVersion7()}"),
            new LookupHash(Guid.CreateVersion7().ToByteArray()),
            Locale.FrenchCameroon,
            "terms-2026-08",
            "privacy-2026-07",
            CodeDigest,
            Guid.CreateVersion7().ToByteArray(),
            startedAt,
            options);
}
