using MoniPay.Kernel;
using MoniPay.Kernel.Http;

namespace MoniPay.Users.Features.CurrentUser;

internal sealed record UserAttributes
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Phone { get; init; }

    public required string Email { get; init; }

    public required string Locale { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

internal sealed record CurrentUserView(
    UserId UserId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    Locale Locale,
    DateTimeOffset CreatedAt);

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
