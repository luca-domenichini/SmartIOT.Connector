using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Core.Util;
using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Tcp.Client;
using SmartIOT.Connector.Tcp.Server;

namespace SmartIOT.Connector.Tcp
{
	public class TcpConnectorFactory : IConnectorFactory
	{
		private readonly string PublishWriteEventsKey = "PublishWriteEvents".ToLower();
		private readonly string ServerKey = "Server".ToLower();
		private readonly string PortKey = "Port".ToLower();
		private readonly string PingIntervalMillisKey = "PingIntervalMillis".ToLower();
		private readonly string ReconnectIntervalMillisKey = "ReconnectIntervalMillis".ToLower();


		public IConnector? CreateConnector(string connectionString)
		{
			var tokens = ConnectionStringParser.ParseTokens(connectionString);

			if (connectionString.ToLower().StartsWith("tcpclient://", StringComparison.InvariantCultureIgnoreCase))
			{
				return new TcpClientConnector(ParseTcpClientConnectorOptions(tokens));
			}
			if (connectionString.ToLower().StartsWith("tcpserver://", StringComparison.InvariantCultureIgnoreCase))
			{
				return new TcpServerConnector(ParseTcpServerConnectorOptions(tokens));
			}

			return null;
		}

		private IStreamMessageSerializer ParseMessageSerializer(IDictionary<string, string> tokens)
		{
			var s = tokens.GetOrDefault("serializer");

			if ("json".Equals(s, StringComparison.InvariantCultureIgnoreCase))
				return new JsonStreamMessageSerializer();

			return new ProtobufStreamMessageSerializer();
		}

		private TcpClientConnectorOptions ParseTcpClientConnectorOptions(IDictionary<string, string> tokens)
		{
			var isPublishWriteEvents = "true".Equals(tokens.GetOrDefault(PublishWriteEventsKey), StringComparison.InvariantCultureIgnoreCase);
			var serverAddress = tokens.GetOrDefault(ServerKey) ?? throw new ArgumentException("Invalid TcpClient connectionString: Server expected");
			var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
			if (!int.TryParse(sServerPort, out var serverPort))
				throw new ArgumentException("Invalid TcpClient connectionString: Port expected");

			string sReconnectIntervalMillis = tokens.GetOrDefault(ReconnectIntervalMillisKey) ?? "5000";
			if (!int.TryParse(sReconnectIntervalMillis, out var reconnectIntervalMillis))
				reconnectIntervalMillis = 5000;

			string sPingIntervalMillis = tokens.GetOrDefault(PingIntervalMillisKey) ?? "0";
			if (!int.TryParse(sPingIntervalMillis, out var pingIntervalMillis))
				pingIntervalMillis = 0;

			return new TcpClientConnectorOptions(isPublishWriteEvents, serverAddress, serverPort, TimeSpan.FromMilliseconds(reconnectIntervalMillis), ParseMessageSerializer(tokens), TimeSpan.FromMilliseconds(pingIntervalMillis));
		}

		private TcpServerConnectorOptions ParseTcpServerConnectorOptions(IDictionary<string, string> tokens)
		{
			var isPublishWriteEvents = "true".Equals(tokens.GetOrDefault(PublishWriteEventsKey), StringComparison.InvariantCultureIgnoreCase);
			var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
			if (!int.TryParse(sServerPort, out var serverPort))
				throw new ArgumentException("Invalid TcpServer connectionString: Port expected");

			string sPingIntervalMillis = tokens.GetOrDefault(PingIntervalMillisKey) ?? "0";
			if (!int.TryParse(sPingIntervalMillis, out var pingIntervalMillis))
				pingIntervalMillis = 0;

			return new TcpServerConnectorOptions(isPublishWriteEvents, serverPort, ParseMessageSerializer(tokens), TimeSpan.FromMilliseconds(pingIntervalMillis));
		}
	}
}
