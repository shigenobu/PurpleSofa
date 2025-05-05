using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests;

public class TestDate
{
    public TestDate(ITestOutputHelper helper)
    {
        PsLogger.Verbose = true;
        // PsLogger.Transfer = msg => helper.WriteLine(msg?.ToString());
    }

    [Fact]
    public void TestNow()
    {
        PsLogger.Debug(PsDate.Now());

        PsDate.AddSeconds = 60 * 60 * 9;
        PsLogger.Debug(PsDate.Now());
        PsLogger.Debug(PsDate.NowTimestampMilliSeconds());
        Assert.True(true);
    }
}