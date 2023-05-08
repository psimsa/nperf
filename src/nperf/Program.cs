using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using nperf;

// Create command line options
var serverOption = new Option<bool>(
    new[] { "--server", "-s" },
    "Start the application in server mode");
var addressOption = new Option<string>(
    new[] { "--address", "-a" },
    () => "0.0.0.0",
    "The IP address to connect to or listen on, can be omitted in server mode");
var portOption = new Option<int>(
    new[] { "--port", "-p" },
    () => 5000,
    "The port to connect to or listen on");
var bufferSize = new Option<int>(
    new[] { "--buffer-size", "-b" },
    () => 1024 * 1024 * 10,
    "The buffer size to use for sending and receiving data");
var rootCommand = new RootCommand("A network bandwidth testing tool")
{
    serverOption,
    addressOption,
    portOption,
    bufferSize,
};

rootCommand.AddValidator(validate =>
{
    var sor = validate.GetValueForOption(serverOption);
    var aor = validate.GetValueForOption(addressOption);
    if (!sor && aor == "0.0.0.0")
    {
        validate.ErrorMessage = "The --address option cannot be omitted in client mode";
    }
});

// Configure command line option handling
rootCommand.SetHandler(Run, serverOption, addressOption, portOption, bufferSize);

// Parse and execute the command line arguments
await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args);

static async Task Run(bool server, string address, int port, int bufferSize)
{
    var appSettings = new AppSettings(bufferSize, address ?? "0.0.0.0", port);

    if (server)
    {
        var serverMode = new ServerMode(appSettings);
        await serverMode.StartServerAsync();
    }
    else
    {
        var clientMode = new ClientMode(appSettings);

        Console.WriteLine("Running large data test...");
        (long dataSize, long duration) largeDataResult = await clientMode.SendLargeDataAsync();
        Console.WriteLine(
            $"Large data test completed in {largeDataResult.duration} ms with {largeDataResult.dataSize} bytes sent");

        Console.WriteLine("Running medium data test...");
        (long dataSize, long duration) mediumDataResult = await clientMode.SendMediumDataAsync();
        Console.WriteLine(
            $"Medium data test completed in {mediumDataResult.duration} ms with {mediumDataResult.dataSize} bytes sent");

        Console.WriteLine("Running small data test...");
        (long dataSize, long duration) smallDataResult = await clientMode.SendSmallDataAsync();
        Console.WriteLine(
            $"Small data test completed in {smallDataResult.duration} ms with {smallDataResult.dataSize} bytes sent");

        Console.WriteLine("\nSummary:");
        PrintBandwidth("Large data test", largeDataResult.dataSize, largeDataResult.duration);
        PrintBandwidth("Medium data test", mediumDataResult.dataSize, mediumDataResult.duration);
        PrintBandwidth("Small data test", smallDataResult.dataSize, smallDataResult.duration);
    }
}

static void PrintBandwidth(string label, long dataSize, long duration)
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

    Console.WriteLine($"{label}: {bandwidth:F2} {units[unitIndex]} / {(bandwidth/8):F2} {unitsBytes[unitIndex]}");
}

public record AppSettings(int BufferSize = 1024 * 1024 * 10, string IpAddress = "0.0.0.0", int Port = 5000,
    bool IsServerMode = false);
