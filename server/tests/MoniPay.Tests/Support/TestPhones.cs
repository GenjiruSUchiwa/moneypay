using System.Globalization;

namespace MoniPay.Tests.Support;

public static class TestPhones
{
    private static long nextCounter = 599_999_999;

    public static string Next()
    {
        long counter = Interlocked.Increment(ref nextCounter);
        return "237" + counter.ToString("D9", CultureInfo.InvariantCulture);
    }
}
