using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;

namespace MoniPay.Sessions.Features.Sessions;

internal sealed record SessionCredentialsAttributes
{
    public required string TokenType { get; init; }

    public required string AccessToken { get; init; }

    public required DateTimeOffset AccessTokenExpiresAt { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

internal sealed record ReadSessionAttributes
{
    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset LastSeenAt { get; init; }

    public required DateTimeOffset AccessTokenExpiresAt { get; init; }
}

internal sealed record CurrentSessionView(
    Guid SessionId,
    UserId UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset AccessTokenExpiresAt);

internal static class SessionResources
{
    public const string BearerTokenType = "Bearer";

    public const string Self = SessionRoutes.Group + SessionRoutes.Current;

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

    public static JsonApiResponseResource<ReadSessionAttributes> Read(CurrentSessionView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new JsonApiResponseResource<ReadSessionAttributes>
        {
            Type = SessionResourceTypes.Sessions,
            Id = view.SessionId.ToString(),
            Attributes = new ReadSessionAttributes
            {
                CreatedAt = view.CreatedAt,
                LastSeenAt = view.LastSeenAt,
                AccessTokenExpiresAt = view.AccessTokenExpiresAt,
            },
            Relationships = User(view.UserId),
            Links = new JsonApiLinks { Self = Self },
        };
    }

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
                Links = new JsonApiLinks { Related = MoniPayRoutes.CurrentUser },
            },
        };
}
