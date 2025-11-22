using Xunit;
using nperf;

namespace nperf.tests;

public class BandwidthFormatterTests
{
    [Theory]
    [InlineData(1000, 1000, "8000.00 bps / 1000.00 Bps")] // 1000 bytes in 1 sec = 8000 bps
    [InlineData(1024, 1000, "8.00 Kbps / 1.00 KBps")] // 1024 bytes in 1 sec = 8192 bits = 8 Kbps
    [InlineData(1024 * 1024, 1000, "8.00 Mbps / 1.00 MBps")] // 1 MB in 1 sec = 8 Mbps
    public void Format_ReturnsCorrectString(long dataSize, long duration, string expected)
    {
        var result = BandwidthFormatter.Format(dataSize, duration);
        Assert.Equal(expected, result);
    }
}
