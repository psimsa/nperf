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
        // Disable kernel buffer for potential zero-copy benefits
        client.ReceiveBufferSize = 0;
        var socket = client.Client;
        string? remoteEndPoint = socket.RemoteEndPoint?.ToString();

        try
        {
            int bytesRead;

            // Use Socket.ReceiveAsync directly instead of NetworkStream
            while ((bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None)) != 0)
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
            Console.WriteLine($"Client disconnected from {remoteEndPoint} (Total received: {totalBytesReceived} bytes)");
        }
    }
}

