using nperf;

AppSettings appSettings;

switch (args[0])
{
    case "server":
        appSettings = new AppSettings(IsServerMode: true);
        break;
    case "client":
        appSettings = new AppSettings(IpAddress: args[1], Port: int.Parse(args[2]));
        break;
    default:
        Console.WriteLine("Please specify server or client mode");
        return;
}

if (appSettings.IsServerMode)
{
    var serverMode = new ServerMode(appSettings);
    await serverMode.StartServerAsync();
}
else
{
    var clientMode = new ClientMode(appSettings);

    Console.WriteLine("Running large data test...");
    (long dataSize, long duration)  largeDataResult  = await clientMode.SendLargeDataAsync();
    Console.WriteLine($"Large data test completed in {largeDataResult.duration} ms with {largeDataResult.dataSize} bytes sent");

    Console.WriteLine("Running medium data test...");
    (long dataSize, long duration)  mediumDataResult  = await clientMode.SendMediumDataAsync();
    Console.WriteLine($"Medium data test completed in {mediumDataResult.duration} ms with {mediumDataResult.dataSize} bytes sent");

    Console.WriteLine("Running small data test...");
    (long dataSize, long duration)  smallDataResult  = await clientMode.SendSmallDataAsync();
    Console.WriteLine($"Small data test completed in {smallDataResult.duration} ms with {smallDataResult.dataSize} bytes sent");

    Console.WriteLine("\nSummary:");
    PrintBandwidth("Large data test", largeDataResult.dataSize, largeDataResult.duration);
    PrintBandwidth("Medium data test", mediumDataResult.dataSize, mediumDataResult.duration);
    PrintBandwidth("Small data test", smallDataResult.dataSize, smallDataResult.duration);

}

void PrintBandwidth(string label, long dataSize, long duration)
{
    double durationInSeconds = duration / 1000.0;
    double dataSizeInBits = dataSize * 8.0;
    double bandwidth = dataSizeInBits / durationInSeconds; // bps
    
    string unit;
    if (bandwidth > 1e9)
    {
        bandwidth /= 1e9;
        unit = "Gbps";
    }
    else if (bandwidth > 1e6)
    {
        bandwidth /= 1e6;
        unit = "Mbps";
    }
    else
    {
        bandwidth /= 1e3;
        unit = "Kbps";
    }

    Console.WriteLine($"{label}: {bandwidth:F2} {unit}");
}

public record AppSettings (int BufferSize = 1024 * 1024 * 10, string IpAddress = "0.0.0.0", int Port = 5000, bool IsServerMode = false);
