﻿using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace nperf;

public class ClientMode
{
    private readonly AppSettings _appSettings;
    // private TcpClient _client;

    public ClientMode(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public async Task<TcpClient> ConnectToServerAsync()
    {
        var client = new TcpClient(){ NoDelay = true };
        await client.ConnectAsync(IPAddress.Parse(_appSettings.IpAddress), _appSettings.Port);
        return client;
    }

    public async Task<(long DataSize, long Duration)> SendLargeDataAsync()
    {
        using var client = await ConnectToServerAsync();
        return await SendDataAsync(client, 1024 * 1024 * 100, 1);
    }

    public async Task<(long DataSize, long Duration)> SendMediumDataAsync()
    {
        using var client = await ConnectToServerAsync();

        return await SendDataAsync(client, 1024 * 1024 * 2, 10);
    }

    public async Task<(long DataSize, long Duration)> SendSmallDataAsync()
    {
        using var client = await ConnectToServerAsync();
        return await SendDataAsync(client, 1024 * 50, 500);
    }

    private async Task<(long DataSize, long Duration)> SendDataAsync(TcpClient _client, int dataSize, int iterations)
    {
        var random = new Random();
        var buffer = new byte[dataSize];
        var networkStream = _client.GetStream();

        _client.SendBufferSize = 0;
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        for (int i = 0; i < iterations; i++)
        {
            random.NextBytes(buffer);
            await networkStream.WriteAsync(buffer, 0, buffer.Length);
            await networkStream.FlushAsync();
        }
        
        stopwatch.Stop();
        return (dataSize * iterations, stopwatch.ElapsedMilliseconds);
    }
}
