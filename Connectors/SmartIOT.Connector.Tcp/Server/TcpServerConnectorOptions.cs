using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Tcp.Server
{
    public class TcpServerConnectorOptions : ConnectorOptions
    {
        public int ServerPort { get; }
        public IStreamMessageSerializer MessageSerializer { get; }
        public TimeSpan PingInterval { get; }

        public TcpServerConnectorOptions(string connectionString, bool isPublishWriteEvents, int serverPort, IStreamMessageSerializer messageSerializer, TimeSpan pingInterval)
            : base(connectionString, isPublishWriteEvents)
        {
            ServerPort = serverPort;
            MessageSerializer = messageSerializer;
            PingInterval = pingInterval;
        }
    }
}
