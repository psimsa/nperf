using System.CommandLine;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;
using PacketDash;

const string DefaultIpAddress = AppSettings.DefaultIpAddress;

// Create command line options
var serverOption = new Option<bool>("--server")
{
    Description = "Start the application in server mode"
};
serverOption.Aliases.Add("-s");

var addressOption = new Option<string>("--address")
{
    Description = "The IP address to connect to or listen on, can be omitted in server mode",
    DefaultValueFactory = _ => DefaultIpAddress
};
addressOption.Aliases.Add("-a");

var portOption = new Option<int>("--port")
{
    Description = "The port to connect to or listen on",
    DefaultValueFactory = _ => 5000
};
portOption.Aliases.Add("-p");

var bufferSize = new Option<int>("--buffer-size")
{
    Description = "The buffer size to use for sending and receiving data",
    DefaultValueFactory = _ => 1024 * 1024 * 10
};
bufferSize.Aliases.Add("-b");

var logVerbose = new Option<bool>("--verbose")
{
    Description = "Enable verbose logging"
};
logVerbose.Aliases.Add("-v");

var rootCommand = new RootCommand("A network bandwidth testing tool")
{
    serverOption,
    addressOption,
    portOption,
    bufferSize,
    logVerbose
};

rootCommand.Validators.Add(validate =>
{
    var sor = validate.GetValue(serverOption);
    var aor = validate.GetValue(addressOption);
    if (!sor && aor == DefaultIpAddress)
    {
        validate.AddError("The --address option cannot be omitted in client mode");
    }
});

// Configure command line option handling
rootCommand.SetAction(async (ParseResult result) =>
{
    var server = result.GetValue(serverOption);
    var address = result.GetValue(addressOption);
    var port = result.GetValue(portOption);
    var buffer = result.GetValue(bufferSize);
    var verbose = result.GetValue(logVerbose);
    await Run(server, address, port, buffer, verbose);
});

// Parse and execute the command line arguments
await rootCommand.Parse(args).InvokeAsync();

static async Task Run(bool server, string address, int port, int bufferSize, bool logVerbose)
{
    var appSettings = new AppSettings(bufferSize, address ?? DefaultIpAddress, port, logVerbose);

    if (server)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate process termination
            cts.Cancel();
        };

        var serverMode = new ServerMode(appSettings);
        await serverMode.StartServerAsync(cts.Token);
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
