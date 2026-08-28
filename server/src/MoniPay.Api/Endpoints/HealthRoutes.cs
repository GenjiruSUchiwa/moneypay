namespace MoniPay.Api.Endpoints;

/// <summary>
/// The health paths. They are contract: a container HEALTHCHECK, a load balancer probe and a
/// deployment manifest all name them. Those live outside the compiler's reach and repeat the
/// literal, so changing a path here means grepping the Dockerfile and docs/ops/ in the same
/// commit — the constant keeps the C# side consistent, not the whole repository.
/// </summary>
public static class HealthRoutes
{
    /// <summary>Liveness: answers whenever the process is up. Runs no check.</summary>
    public const string Live = "/health";

    /// <summary>Readiness: also asks the database. Fails while a dependency is down.</summary>
    public const string Ready = "/health/ready";
}
