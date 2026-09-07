using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Persistence;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Persistence;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Domain;

/// <summary>
/// The token service against PostgreSQL: rotation consumes the presented link, a replay revokes
/// the whole family, and every refused refresh is answered with one problem type whatever the
/// reason the log carries.
/// </summary>
public sealed class SessionTokenServiceTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private async Task<SessionTokenResult> CreateSessionAsync(UserId? userId = null, Guid? deviceId = null)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        SessionTokenService service = scope.ServiceProvider.GetRequiredService<SessionTokenService>();
        return await service.CreateAsync(
            userId ?? UserId.New(),
            deviceId ?? Guid.CreateVersion7(),
            Cancellation);
    }

    private async Task<SessionTokenResult> RefreshAsync(string refreshToken, Guid deviceId)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        SessionTokenService service = scope.ServiceProvider.GetRequiredService<SessionTokenService>();
        return await service.RefreshAsync(refreshToken, deviceId, Cancellation);
    }

    private async Task<SessionTokenResult> ReplaceBootstrapAsync(UserId userId, Guid priorSessionId, Guid deviceId)
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        SessionTokenService service = scope.ServiceProvider.GetRequiredService<SessionTokenService>();
        return await service.ReplaceBootstrapAsync(userId, priorSessionId, deviceId, Cancellation);
    }

    [Fact]
    public async Task A_refresh_consumes_the_presented_token_and_returns_a_new_one()
    {
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await CreateSessionAsync(deviceId: deviceId);

        SessionTokenResult refreshed = await RefreshAsync(created.RefreshToken, deviceId);

        Assert.Equal(created.SessionId, refreshed.SessionId);
        Assert.NotEqual(created.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(created.AccessToken.Value, refreshed.AccessToken.Value);

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        RefreshTokenFactory refreshTokens = scope.ServiceProvider.GetRequiredService<RefreshTokenFactory>();
        RefreshToken consumed = await database.RefreshTokens.SingleAsync(
            token => token.TokenDigest == refreshTokens.Digest(created.RefreshToken),
            Cancellation);
        Assert.NotNull(consumed.UsedAt);
        RefreshToken replacement = await database.RefreshTokens.SingleAsync(
            token => token.Id == consumed.ReplacedById,
            Cancellation);
        Assert.Equal(refreshTokens.Digest(refreshed.RefreshToken), replacement.TokenDigest);
        Assert.Null(replacement.UsedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replayed_token_revokes_the_family_and_refuses_the_replacement(bool runCleanup)
    {
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await CreateSessionAsync(deviceId: deviceId);
        Api.Time.Advance(TimeSpan.FromSeconds(1));
        SessionTokenResult refreshed = await RefreshAsync(created.RefreshToken, deviceId);
        if (runCleanup)
        {
            Api.Time.Advance(TimeSpan.FromDays(1));
            await Api.RunCleanupCycleAsync(Cancellation);
        }

        RefusalException replay = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(created.RefreshToken, deviceId));
        Assert.Equal(MoniPayErrorTypes.RefreshTokenReused, replay.Type);

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session presented = await database.Sessions.SingleAsync(
            session => session.Id == created.SessionId,
            Cancellation);
        Assert.Equal(SessionRevokeReason.RefreshTokenReuse, presented.RevokeReason);
        List<Session> family = await database.Sessions
            .Where(session => session.TokenFamilyId == presented.TokenFamilyId)
            .ToListAsync(Cancellation);
        Assert.All(family, session => Assert.Equal(SessionRevokeReason.RefreshTokenReuse, session.RevokeReason));

        RefusalException afterRevocation = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(refreshed.RefreshToken, deviceId));
        Assert.Equal(MoniPayErrorTypes.SessionInvalid, afterRevocation.Type);
    }

    [Fact]
    public async Task An_unknown_token_and_an_expired_token_are_refused_as_invalid()
    {
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await CreateSessionAsync(deviceId: deviceId);

        string unknown = new RefreshTokenFactory().Create().Raw;
        RefusalException unknownRefusal = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(unknown, deviceId));
        Assert.Equal(MoniPayErrorTypes.SessionInvalid, unknownRefusal.Type);

        Api.Time.Advance(TimeSpan.FromDays(31));
        RefusalException expiredRefusal = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(created.RefreshToken, deviceId));
        Assert.Equal(MoniPayErrorTypes.SessionInvalid, expiredRefusal.Type);
    }

    [Fact]
    public async Task A_foreign_device_and_a_revoked_session_are_refused_as_invalid()
    {
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await CreateSessionAsync(deviceId: deviceId);

        RefusalException foreignDevice = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(created.RefreshToken, Guid.CreateVersion7()));
        Assert.Equal(MoniPayErrorTypes.SessionInvalid, foreignDevice.Type);

        await using (AsyncServiceScope scope = Api.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SessionTokenService>()
                .RevokeAsync(created.SessionId, Cancellation);
        }

        RefusalException revoked = await Assert.ThrowsAsync<RefusalException>(
            () => RefreshAsync(created.RefreshToken, deviceId));
        Assert.Equal(MoniPayErrorTypes.SessionInvalid, revoked.Type);
    }

    [Fact]
    public async Task A_repeated_completion_replaces_the_bootstrap_session_in_a_new_family()
    {
        UserId userId = UserId.New();
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult bootstrap = await CreateSessionAsync(userId, deviceId);

        SessionTokenResult replacement = await ReplaceBootstrapAsync(userId, bootstrap.SessionId, deviceId);

        Assert.NotEqual(bootstrap.SessionId, replacement.SessionId);
        Assert.NotEqual(bootstrap.AccessToken.Value, replacement.AccessToken.Value);

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session prior = await database.Sessions.SingleAsync(
            session => session.Id == bootstrap.SessionId,
            Cancellation);
        Session current = await database.Sessions.SingleAsync(
            session => session.Id == replacement.SessionId,
            Cancellation);
        Assert.Equal(SessionRevokeReason.BootstrapReplaced, prior.RevokeReason);
        Assert.NotNull(prior.RevokedAt);
        Assert.NotEqual(prior.TokenFamilyId, current.TokenFamilyId);
    }

    [Fact]
    public async Task A_repeated_completion_cannot_replace_another_users_session()
    {
        SessionTokenResult bootstrap = await CreateSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ReplaceBootstrapAsync(UserId.New(), bootstrap.SessionId, Guid.CreateVersion7()));

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session prior = await database.Sessions.SingleAsync(
            session => session.Id == bootstrap.SessionId,
            Cancellation);
        Assert.True(prior.IsActive);
    }

    [Fact]
    public async Task Two_parallel_refreshes_of_one_token_produce_one_success_and_one_refusal()
    {
        Guid deviceId = Guid.CreateVersion7();
        SessionTokenResult created = await CreateSessionAsync(deviceId: deviceId);
        await using AsyncServiceScope first = Api.Services.CreateAsyncScope();
        await using AsyncServiceScope second = Api.Services.CreateAsyncScope();
        SessionTokenService firstService = first.ServiceProvider.GetRequiredService<SessionTokenService>();
        SessionTokenService secondService = second.ServiceProvider.GetRequiredService<SessionTokenService>();

        Task<SessionTokenResult> firstAttempt = firstService.RefreshAsync(created.RefreshToken, deviceId, Cancellation);
        Task<SessionTokenResult> secondAttempt = secondService.RefreshAsync(created.RefreshToken, deviceId, Cancellation);

        SessionTokenResult? winner = null;
        SessionTokenResult? loser = null;
        RefusalException? refusal = null;
        try
        {
            winner = await firstAttempt;
        }
        catch (RefusalException caught)
        {
            refusal = caught;
        }

        try
        {
            loser = await secondAttempt;
        }
        catch (RefusalException caught)
        {
            refusal = caught;
        }

        Assert.NotNull(refusal);
        Assert.Equal(MoniPayErrorTypes.RefreshTokenReused, refusal.Type);
        Assert.True(winner is null ^ loser is null, "exactly one refresh succeeds");

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session presented = await database.Sessions.SingleAsync(
            session => session.Id == created.SessionId,
            Cancellation);
        List<Session> family = await database.Sessions
            .Where(session => session.TokenFamilyId == presented.TokenFamilyId)
            .ToListAsync(Cancellation);
        Assert.All(family, session => Assert.Equal(SessionRevokeReason.RefreshTokenReuse, session.RevokeReason));
    }

    [Fact]
    public async Task The_credential_result_never_prints_either_token()
    {
        SessionTokenResult created = await CreateSessionAsync();

        Assert.Equal(nameof(SessionTokenResult), created.ToString());
    }

    [Fact]
    public async Task Only_the_digest_is_stored()
    {
        SessionTokenResult created = await CreateSessionAsync();

        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        RefreshTokenFactory refreshTokens = scope.ServiceProvider.GetRequiredService<RefreshTokenFactory>();
        RefreshToken stored = await database.RefreshTokens.SingleAsync(
            token => token.SessionId == created.SessionId,
            Cancellation);

        Assert.Equal(32, stored.TokenDigest.Length);
        Assert.Equal(refreshTokens.Digest(created.RefreshToken), stored.TokenDigest);
    }

    [Fact]
    public async Task Revoking_twice_keeps_the_first_reason_and_an_unknown_session_changes_nothing()
    {
        SessionTokenResult created = await CreateSessionAsync();
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        SessionTokenService service = scope.ServiceProvider.GetRequiredService<SessionTokenService>();

        await service.RevokeAsync(created.SessionId, Cancellation);
        await service.RevokeAsync(created.SessionId, Cancellation);
        await service.RevokeAsync(Guid.CreateVersion7(), Cancellation);

        MoniPayDbContext database = scope.ServiceProvider.GetRequiredService<MoniPayDbContext>();
        Session session = await database.Sessions.SingleAsync(
            candidate => candidate.Id == created.SessionId,
            Cancellation);
        Assert.Equal(SessionRevokeReason.UserRequest, session.RevokeReason);
    }
}
