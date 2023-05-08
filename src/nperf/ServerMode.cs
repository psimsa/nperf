using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace nperf;

public class ServerMode
{
    private readonly AppSettings _appSettings;

    public ServerMode(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public async Task StartServerAsync()
    {
        var listener = new TcpListener(IPAddress.Parse(_appSettings.IpAddress), _appSettings.Port);
        listener.Start();

        Console.WriteLine($"Server listening on {_appSettings.IpAddress}:{_appSettings.Port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");

            _ = HandleClientAsync(client); // Fire and forget, we don't need to await this task
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        long totalBytesReceived = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(_appSettings.BufferSize);
        var stream = client.GetStream();

        try
        {
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                totalBytesReceived += bytesRead;
                if(_appSettings.LogVerbose) 
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} received {bytesRead} bytes (Total: {totalBytesReceived})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing client: {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            client.Dispose();
            Console.WriteLine($"Client disconnected from {client.Client.RemoteEndPoint} (Total received: {totalBytesReceived} bytes)");
        }
    }
}

