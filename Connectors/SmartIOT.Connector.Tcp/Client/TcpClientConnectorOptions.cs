using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Tcp.Client;

public class TcpClientConnectorOptions : ConnectorOptions
{
    public string ServerAddress { get; }
    public int ServerPort { get; }
    public TimeSpan ReconnectInterval { get; }
    public IStreamMessageSerializer MessageSerializer { get; }
    public TimeSpan PingInterval { get; }

    public TcpClientConnectorOptions(string connectionString, bool isPublishWriteEvents, string serverAddress, int serverPort, TimeSpan reconnectInterval, IStreamMessageSerializer messageSerializer, TimeSpan pingInterval)
        : base(connectionString, isPublishWriteEvents)
    {
        ServerAddress = serverAddress;
        ServerPort = serverPort;
        ReconnectInterval = reconnectInterval;
        MessageSerializer = messageSerializer;
        PingInterval = pingInterval;
    }
}
