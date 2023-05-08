# nperf
Super simple network performance tester tool. Available as linux binary or a dotnet tool

## Installation (dotnet tool)
```
$ dotnet tool install -g dotnet-nperf
```

## Usage
For help:
```
$ nperf --help
```
---
#### Basic usage is as such:

On first computer, start a server:
```
nperf -s [-a <listen_address>] [-p <listen_port>]
```
By default starts a server listening on all interfaces and port 5000 (you can override address and port).

On second computer, start a client that does the testing:
```
nperf -a <server_ip> [-p <server_port>]
```

Starts a client that connects to the server and sends three batches of data to the server:
- 100MB in 1 iteration
- 2MB in 10 iterations
- 50KB in 500 iterations

At the end, the clients statistics as such:
```
Summary:
Running large data test...
Large data test completed in 8920 ms with 104857600 bytes sent
Running medium data test...
Medium data test completed in 1822 ms with 20971520 bytes sent
Running small data test...
Small data test completed in 3448 ms with 25600000 bytes sent

Summary:
Large data test: 89,69 Mbps / 11,21 MBps
Medium data test: 87,82 Mbps / 10,98 MBps
Small data test: 56,65 Mbps / 7,08 MBps
```
