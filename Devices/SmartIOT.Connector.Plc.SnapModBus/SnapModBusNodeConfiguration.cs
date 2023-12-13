using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Plc.SnapModBus;

public class SnapModBusNodeConfiguration : DeviceConfiguration
{
    public string IpAddress { get; }
    public int Port { get; }
    public byte NodeId { get; }
    public bool SwapBytes { get; }

    public SnapModBusNodeConfiguration(DeviceConfiguration configuration) : base(configuration)
    {
        var tokens = ConnectionStringParser.ParseTokens(configuration.ConnectionString);

        IpAddress = tokens.GetOrDefault("ip")!; // suppressing null-warning: check is next line
        if (string.IsNullOrWhiteSpace(IpAddress))
            throw new ArgumentException("Ip not found in ConnectionString. Provide a valid IpAddress or HostName");

        var sPort = tokens.GetOrDefault("port") ?? "502";
        if (!int.TryParse(sPort, out var port))
            throw new ArgumentException($"Port not valid in ConnectionString. Provide a valid number");
        Port = port;

        var sNodeId = tokens.GetOrDefault("nodeid") ?? "1";
        if (!byte.TryParse(sNodeId, out var nodeId))
            throw new ArgumentException($"NodeId not valid in ConnectionString. Provide a valid byte.");
        NodeId = nodeId;

        var sSwapBytes = tokens.GetOrDefault("swapbytes") ?? "false";
        if (bool.TryParse(sSwapBytes, out var swapBytes))
            SwapBytes = swapBytes;
    }
}
