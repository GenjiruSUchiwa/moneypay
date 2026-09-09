using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Users.Features.CurrentUser;

/// <summary>
/// The attributes the current-user read returns: the profile completion stored, without hashes,
/// ciphertext, consents, or credentials. The phone stays digits-only E.164 and the email stays in
/// its display form, exactly as registration normalized them.
/// </summary>
internal sealed record UserAttributes
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Phone { get; init; }

    public required string Email { get; init; }

    public required string Locale { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>What a read of the current user returns: the stored profile with its contact data unprotected.</summary>
internal sealed record CurrentUserView(
    UserId UserId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    Locale Locale,
    DateTimeOffset CreatedAt);

/// <summary>Projects the current-user view onto the <c>users</c> resource and its self link.</summary>
internal static class UserResources
{
    public static JsonApiResponseResource<UserAttributes> FromView(CurrentUserView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new JsonApiResponseResource<UserAttributes>
        {
            Type = UserResourceTypes.Users,
            Id = view.UserId.Value.ToString(),
            Attributes = new UserAttributes
            {
                FirstName = view.FirstName,
                LastName = view.LastName,
                Phone = view.Phone,
                Email = view.Email,
                Locale = view.Locale.Value,
                CreatedAt = view.CreatedAt,
            },
            Links = new JsonApiLinks { Self = MoniPayRoutes.CurrentUser },
        };
    }
}
