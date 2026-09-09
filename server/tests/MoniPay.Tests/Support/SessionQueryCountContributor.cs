using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel;

namespace MoniPay.Tests.Support;

internal sealed class SessionQueryCountContributor(SessionQueryCounter counter) : IDbContextOptionsContributor
{
    public void Contribute(DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddInterceptors([counter]);
    }
}
