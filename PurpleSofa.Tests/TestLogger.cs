using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests
{
    public class TestLogger
    {
        public TestLogger(ITestOutputHelper helper)
        {
            PsLogger.Verbose = true;
            PsLogger.Transfer = msg => helper.WriteLine(msg?.ToString());
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
        public void TestFileOut()
        {
            PsLogger.Writer = new StreamWriter(new FileStream("PurpleSofa.log", FileMode.Append));
            PsLogger.Error("This is Error.");
            PsLogger.Info("This is Info.");
            PsLogger.Debug("This is Debug.");
            Assert.True(true);
        }
    }
}