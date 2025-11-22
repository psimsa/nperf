# PacketDash

**PacketDash** is a high-performance, simple network bandwidth testing tool. It is available as a Linux binary or a .NET tool.

## Key Features
- **High Performance**: Optimized socket handling and zero-copy buffering for maximum throughput.
- **Reliable**: Robust connection handling and error reporting.
- **Simple**: Easy-to-use command-line interface.
- **Cross-Platform**: Runs on Windows, Linux, and macOS via .NET 8.

## Installation (dotnet tool)
```bash
$ dotnet tool install -g PacketDash
```

## Usage

### Basic Usage

**1. Start the Server**
On the first machine, start the server:
```bash
pdash -s [-a <listen_address>] [-p <listen_port>]
```
*   By default, it listens on `0.0.0.0` (all interfaces) and port `5000`.

**2. Start the Client**
On the second machine, connect to the server:
```bash
pdash -a <server_ip> [-p <server_port>]
```

### What it does
The client connects to the server and sends three batches of data to measure throughput:
1.  **Large Data**: 100MB in 1 iteration (measures sustained throughput).
2.  **Medium Data**: 2MB in 10 iterations (measures bursty throughput).
3.  **Small Data**: 50KB in 500 iterations (measures transaction overhead).

### Example Output
```text
Running large data test...
Large data test completed in 34 ms with 104857600 bytes sent
Running medium data test...
Medium data test completed in 16 ms with 20971520 bytes sent
Running small data test...
Small data test completed in 19 ms with 25600000 bytes sent

Summary:
Large data test: 22.98 Gbps / 2.87 GBps
Medium data test: 10.42 Gbps / 1.30 GBps
Small data test: 11.92 Gbps / 1.49 GBps
```

## Development

### Build
```bash
dotnet build PacketDash.sln
```

### Test
```bash
dotnet test PacketDash.sln
```
