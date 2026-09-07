using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;

namespace MoniPay.Tests.Support;

/// <summary>
/// Feeds the query counter to the shared context through the options-contributor seam the data
/// module already exposes, so the test project never configures the context itself.
/// </summary>
internal sealed class SessionQueryCountContributor(SessionQueryCounter counter) : IDbContextOptionsContributor
{
    public void Contribute(DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddInterceptors([counter]);
    }
}
