using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests;

public class TestLogger
{
    public TestLogger(ITestOutputHelper helper)
    {
        PsLogger.Verbose = true;
        PsLogger.Transfer = new PsLoggerTransfer
        {
            Transfer = msg => helper.WriteLine(msg.ToString()),
            Raw = false
        };
    }

    [Fact]
    public void TestLog()
    {
        PsLogger.Error("This is Error.");
        PsLogger.Info("This is Info.");
        PsLogger.Debug("This is Debug.");
        Assert.True(true);
    }

    [Fact]
    public void TestStop()
    {
        PsLogger.StopLogger = true;
        PsLogger.Error("This is Error.");
        PsLogger.Error(new Exception("This is Exception."));
        PsLogger.Info("This is Info.");
        PsLogger.Debug("This is Debug.");
        Assert.True(true);
    }

    [Fact]
    public void TestFileOut()
    {
        PsLogger.Transfer = null;
        PsLogger.Writer = new StreamWriter(new FileStream("PurpleSofa.log", FileMode.Append));
        PsLogger.Error("This is Error.");
        PsLogger.Info("This is Info.");
        PsLogger.Debug("This is Debug.");
        Assert.True(true);
    }
}