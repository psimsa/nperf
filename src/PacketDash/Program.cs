using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using PacketDash;

const string DefaultIpAddress = AppSettings.DefaultIpAddress;

// Create command line options
var serverOption = new Option<bool>(
    new[] { "--server", "-s" },
    "Start the application in server mode");
var addressOption = new Option<string>(
    new[] { "--address", "-a" },
    () => DefaultIpAddress,
    "The IP address to connect to or listen on, can be omitted in server mode");
var portOption = new Option<int>(
    new[] { "--port", "-p" },
    () => 5000,
    "The port to connect to or listen on");
var bufferSize = new Option<int>(
    new[] { "--buffer-size", "-b" },
    () => 1024 * 1024 * 10,
    "The buffer size to use for sending and receiving data");
var logVerbose = new Option<bool>(
       new[] { "--verbose", "-v" },
          "Enable verbose logging");

var rootCommand = new RootCommand("A network bandwidth testing tool")
{
    serverOption,
    addressOption,
    portOption,
    bufferSize,
    logVerbose
};

rootCommand.AddValidator(validate =>
{
    var sor = validate.GetValueForOption(serverOption);
    var aor = validate.GetValueForOption(addressOption);
    if (!sor && aor == DefaultIpAddress)
    {
        validate.ErrorMessage = "The --address option cannot be omitted in client mode";
    }
});

// Configure command line option handling
rootCommand.SetHandler(Run, serverOption, addressOption, portOption, bufferSize, logVerbose);

// Parse and execute the command line arguments
await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args);

static async Task Run(bool server, string address, int port, int bufferSize, bool logVerbose)
{
    var appSettings = new AppSettings(bufferSize, address ?? DefaultIpAddress, port, logVerbose);

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
        Console.WriteLine($"Large data test: {BandwidthFormatter.Format(largeDataResult.dataSize, largeDataResult.duration)}");
        Console.WriteLine($"Medium data test: {BandwidthFormatter.Format(mediumDataResult.dataSize, mediumDataResult.duration)}");
        Console.WriteLine($"Small data test: {BandwidthFormatter.Format(smallDataResult.dataSize, smallDataResult.duration)}");
    }
}
