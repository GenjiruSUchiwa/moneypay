using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions;

/// <summary>
/// The attributes a session carries in the one response that hands its credentials over: the
/// creation of a session, a refresh, or a sign-up completion. Every other representation of a
/// session omits them — a credential is returned once and never read back.
/// </summary>
internal sealed record SessionCredentialsAttributes
{
    /// <summary>The scheme an access credential is presented under. Always <c>Bearer</c>.</summary>
    public required string TokenType { get; init; }

    public required string AccessToken { get; init; }

    public required DateTimeOffset AccessTokenExpiresAt { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

/// <summary>
/// Projects the credentials a session operation produced onto the <c>sessions</c> resource. Both
/// the refresh and the completion response are built here, so the two cannot drift apart.
/// </summary>
internal static class SessionResources
{
    /// <summary>The <c>tokenType</c> value of every access credential this API issues.</summary>
    public const string BearerTokenType = "Bearer";

    /// <summary>The canonical URI of the session of the calling credential.</summary>
    public const string Self = SessionRoutes.Group + SessionRoutes.Current;

    /// <summary>The URI of the user a session belongs to. The Users module owns that route.</summary>
    public const string CurrentUser = "/users/me";

    public static JsonApiResponseResource<SessionCredentialsAttributes> Credentials(SessionTokenResult session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new JsonApiResponseResource<SessionCredentialsAttributes>
        {
            Type = SessionResourceTypes.Sessions,
            Id = session.SessionId.ToString(),
            Attributes = new SessionCredentialsAttributes
            {
                TokenType = BearerTokenType,
                AccessToken = session.AccessToken.Value,
                AccessTokenExpiresAt = session.AccessTokenExpiresAt,
                RefreshToken = session.RefreshToken,
                RefreshTokenExpiresAt = session.RefreshTokenExpiresAt,
            },
            Relationships = User(session.UserId),
            Links = new JsonApiLinks { Self = Self },
        };
    }

    /// <summary>
    /// The <c>user</c> relationship of a session: the related resource's canonical URI and its
    /// identifier. The identifier travels in the relationship, never as an attribute.
    /// </summary>
    private static IReadOnlyDictionary<string, JsonApiRelationship> User(UserId userId) =>
        new Dictionary<string, JsonApiRelationship>(StringComparer.Ordinal)
        {
            ["user"] = new()
            {
                Data = new JsonApiResourceIdentifier
                {
                    Type = SessionResourceTypes.Users,
                    Id = userId.Value.ToString(),
                },
                Links = new JsonApiLinks { Related = CurrentUser },
            },
        };
}
