using Microsoft.EntityFrameworkCore;

namespace MoniPay.Kernel;

/// <summary>
/// Declares a change to the shared <c>DbContext</c> options: an interceptor, a convention, any
/// extension the context itself must not know by name. A module implements it, registers the
/// implementation, and <c>MoniPay.Data</c> consumes every registration when it builds the
/// options — so the data project never references a module, and a module never opens another
/// module's pipeline.
/// </summary>
public interface IDbContextOptionsContributor
{
    /// <summary>Applies this contribution to the options the shared context is built with.</summary>
    void Contribute(DbContextOptionsBuilder options);
}
