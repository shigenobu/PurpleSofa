using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace PurpleSofa.Tests;

public class TestNetwork
{
    public TestNetwork(ITestOutputHelper helper)
    {
        PsLogger.Verbose = true;
        // PsLogger.Transfer = new PsLoggerTransfer
        // {
        //     Transfer = msg => helper.WriteLine(msg.ToString()),
        //     Raw = true
        // };
    }

    [Fact]
    public void TestIpv4()
    {
        var ip = PsNetwork.GetLocalIpv4Addresses().FirstOrDefault();
        PsLogger.Debug(ip);
    }
    
    [Fact]
    public void TestIpv6()
    {
        var ip = PsNetwork.GetLocalIpv6Addresses().FirstOrDefault();
        PsLogger.Debug(ip);
    }
}