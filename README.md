# nperf
Super simple network performance tester tool

## Usage
```
$ ./nperf server
```
starts a server listening on port 5000. You can also redirect the output to `/dev/null` to avoid the output to the console.

```
$ ./nperf client <server_ip> <server_port>
```

Starts a client that connects to the server and sends three batches of data to the server:
- 100MB in 1 iteration
- 2MB in 10 iterations
- 50KB in 500 iterations

At the end, the clients statistics as such:
```
Running large data test...
Large data test completed in 9790 ms with 104857600 bytes sent
Running medium data test...
Medium data test completed in 1999 ms with 20971520 bytes sent
Running small data test...
Small data test completed in 4047 ms with 25600000 bytes sent

Summary:
Large data test: 85,69 Mbps
Medium data test: 83,93 Mbps
Small data test: 50,61 Mbps
```
