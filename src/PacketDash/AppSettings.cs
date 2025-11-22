namespace PacketDash;

public record AppSettings(int BufferSize = 1024 * 1024 * 10, string IpAddress = AppSettings.DefaultIpAddress, int Port = 5000,
    bool LogVerbose = false)
{
    public const string DefaultIpAddress = "0.0.0.0";
}
