using Microsoft.EntityFrameworkCore;

namespace MoniPay.Kernel;

public interface IDbContextOptionsContributor
{
    void Contribute(DbContextOptionsBuilder options);
}
