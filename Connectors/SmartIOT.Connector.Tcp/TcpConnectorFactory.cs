using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Core.Util;
using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Tcp.Client;

namespace SmartIOT.Connector.Tcp
{
	public class TcpConnectorFactory : IConnectorFactory
	{
		private readonly string PublishWriteEventsKey = "PublishWriteEvents".ToLower();
		private readonly string ServerKey = "Server".ToLower();
		private readonly string PortKey = "Port".ToLower();


		public IConnector? CreateConnector(string connectionString)
		{
			var tokens = ConnectionStringParser.ParseTokens(connectionString);

			if (connectionString.ToLower().StartsWith("tcpclient://", StringComparison.InvariantCultureIgnoreCase))
			{
				return new TcpConnector(ParseConnectorOptions(tokens), new TcpClientEventPublisher(ParseMessageSerializer(tokens), ParseTcpClientEventPublisherOptions(tokens)));
			}
			if (connectionString.ToLower().StartsWith("tcpserver://", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new NotImplementedException("to be done");
			}

			return null;
		}

		private ConnectorOptions ParseConnectorOptions(IDictionary<string, string> tokens)
		{
			return new ConnectorOptions()
			{
				IsPublishWriteEvents = "true".Equals(tokens.GetOrDefault(PublishWriteEventsKey), StringComparison.InvariantCultureIgnoreCase)
			};
		}

		private IStreamMessageSerializer ParseMessageSerializer(IDictionary<string, string> tokens)
		{
			var s = tokens.GetOrDefault("serializer");

			if ("protobuf".Equals(s, StringComparison.InvariantCultureIgnoreCase))
				return new ProtobufStreamMessageSerializer();

			return new JsonStreamMessageSerializer();
		}

		private TcpClientEventPublisherOptions ParseTcpClientEventPublisherOptions(IDictionary<string, string> tokens)
		{
			var serverAddress = tokens.GetOrDefault(ServerKey) ?? throw new ArgumentException("Invalid mqttClient connectionString: Server expected");
			var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
			if (!int.TryParse(sServerPort, out var serverPort))
				throw new ArgumentException("Invalid mqttClient connectionString: Port expected");

			return new TcpClientEventPublisherOptions(serverAddress, serverPort);
		}

		//private TcpServerEventPublisherOptions ParseTcpServerEventPublisherOptions(IDictionary<string, string> tokens)
		//{
		//	var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
		//	if (!int.TryParse(sServerPort, out var serverPort))
		//		throw new ArgumentException("Invalid mqttServer connectionString: port expected");

		//	return new TcpServerEventPublisherOptions(serverPort);
		//}
	}
}
