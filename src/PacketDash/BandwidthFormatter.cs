namespace PacketDash;

public static class BandwidthFormatter
{
    public static string Format(long dataSize, long duration)
    {
        double durationInSeconds = duration / 1000.0;
        double dataSizeInBits = dataSize * 8.0;
        double bandwidth = dataSizeInBits / durationInSeconds; // bps

        int unitIndex = 0;
        var units = new[] { "bps", "Kbps", "Mbps", "Gbps" };
        var unitsBytes = new[] { "Bps", "KBps", "MBps", "GBps" };

        while (bandwidth >= 1024 && unitIndex < units.Length - 1)
        {
            bandwidth /= 1024;
            ++unitIndex;
        }

        return $"{bandwidth:F2} {units[unitIndex]} / {(bandwidth / 8):F2} {unitsBytes[unitIndex]}";
    }
}
