
# nperf Repository Overview

## Project Description
nperf is a simple network performance testing tool available as a Linux binary or a .NET tool. It allows users to test network bandwidth by sending different sizes of data between a client and server.

## File Structure
- **README.md**: Contains installation and usage instructions
- **solution.sln**: Visual Studio solution file
- **src/nperf/**: Contains the main application code
  - **Program.cs**: Main entry point with command-line parsing
  - **ServerMode.cs**: Server implementation that listens for connections
  - **ClientMode.cs**: Client implementation that sends test data
  - **nperf.csproj**: .NET project file with configuration

## How to Run
1. Install as a .NET tool: `dotnet tool install -g dotnet-nperf`
2. Start server: `nperf -s [-a <listen_address>] [-p <listen_port>]`
3. Run client: `nperf -a <server_ip> [-p <server_port>]`

## Testing
The tool runs three tests automatically:
- Large data test: 100MB in 1 iteration
- Medium data test: 2MB in 10 iterations
- Small data test: 50KB in 500 iterations

## Development
- Built with .NET 8.0
- Uses System.CommandLine for command-line parsing
- Implements TCP sockets for network communication
- Supports verbose logging with `-v` flag
