using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests;

public class MoneyTests
{
    [Fact]
    public void A_top_up_adds_whole_francs_because_XAF_has_no_minor_unit()
    {
        Money balance = Money.Xaf(20_000);

        Money afterTopUp = balance + Money.Xaf(25);
        Assert.Equal(20_025, afterTopUp.MinorUnits);
        Assert.Equal(20_025m, afterTopUp.ToDecimal());
    }

    [Fact]
    public void A_card_amount_in_cents_reads_back_as_dollars()
    {
        Money card = Money.Usd(2_500);

        Assert.Equal(25m, card.ToDecimal());
    }

    [Fact]
    public void Two_currencies_never_combine()
    {
        Assert.Throws<InvalidOperationException>(() => Money.Xaf(1_000) + Money.Usd(100));
    }
}
