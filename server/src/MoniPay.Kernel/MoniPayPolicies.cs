namespace MoniPay.Kernel;

/// <summary>Authorization policy names shared by endpoint groups.</summary>
public static class MoniPayPolicies
{
    /// <summary>Requires a valid registration credential bound to the sign-up.</summary>
    public const string Registration = nameof(Registration);

    /// <summary>Requires an active authenticated user session.</summary>
    public const string AuthenticatedUser = nameof(AuthenticatedUser);
}
