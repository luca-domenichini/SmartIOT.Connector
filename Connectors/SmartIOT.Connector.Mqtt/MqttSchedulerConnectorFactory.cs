using SmartIOT.Connector.Mqtt.Client;
using SmartIOT.Connector.Mqtt.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Mqtt
{
	public class MqttSchedulerConnectorFactory : IConnectorFactory
	{
		private const string DefaultExceptionTopicPattern = "exceptions";
		private const string DefaultDeviceStatusEventsTopicPattern = "deviceStatus/device${DeviceId}";
		private const string DefaultTagReadEventsTopicPattern = "tagRead/device${DeviceId}/tag${TagId}";
		private const string DefaultTagWriteRequestCommandsTopicRoot = "tagWrite";

		private readonly string PublishWriteEventsKey = "PublishWriteEvents".ToLower();
		private readonly string ServerKey = "Server".ToLower();
		private readonly string PortKey = "Port".ToLower();
		private readonly string ClientIdKey = "ClientId".ToLower();
		private readonly string ServerIdKey = "ServerId".ToLower();
		private readonly string ExceptionTopicKey = "ExceptionTopic".ToLower();
		private readonly string DeviceStatusEventsTopicKey = "DeviceStatusEventsTopic".ToLower();
		private readonly string TagReadEventsTopicKey = "TagReadEventsTopic".ToLower();
		private readonly string TagWriteRequestCommandsTopicRootKey = "TagWriteRequestCommandsTopicRoot".ToLower();
		private readonly string PublishPartialReadsKey = "PublishPartialReads".ToLower();
		private readonly string UsernameKey = "Username".ToLower();
		private readonly string PasswordKey = "Password".ToLower();


		public IConnector? CreateConnector(string connectionString)
		{
			var tokens = ConnectionStringParser.ParseTokens(connectionString);

			if (connectionString.ToLower().StartsWith("mqttclient://", StringComparison.InvariantCultureIgnoreCase))
			{
				return new MqttSchedulerConnector(ParseConnectorOptions(tokens), new MqttClientEventPublisher(ParseMessageSerializer(tokens), ParseMqttClientEventPublisherOptions(tokens)));
			}
			if (connectionString.ToLower().StartsWith("mqttserver://", StringComparison.InvariantCultureIgnoreCase))
			{
				return new MqttSchedulerConnector(ParseConnectorOptions(tokens), new MqttServerEventPublisher(ParseMessageSerializer(tokens), ParseMqttServerEventPublisherOptions(tokens)));
			}

			return null;
		}

		private MqttSchedulerConnectorOptions ParseConnectorOptions(IDictionary<string, string> tokens)
		{
			return new MqttSchedulerConnectorOptions()
			{
				IsPublishWriteEvents = "true".Equals(tokens.GetOrDefault(PublishWriteEventsKey), StringComparison.InvariantCultureIgnoreCase)
			};
		}

		private IMessageSerializer ParseMessageSerializer(IDictionary<string, string> tokens)
		{
			var s = tokens.GetOrDefault("serializer");

			if ("protobuf".Equals(s, StringComparison.InvariantCultureIgnoreCase))
				return new ProtobufMessageSerializer();

			return new JsonMessageSerializer();
		}

		private MqttClientEventPublisherOptions ParseMqttClientEventPublisherOptions(IDictionary<string, string> tokens)
		{
			var clientId = tokens.GetOrDefault(ClientIdKey) ?? Guid.NewGuid().ToString("N");
			var serverAddress = tokens.GetOrDefault(ServerKey) ?? throw new ArgumentException("Invalid mqttClient connectionString: Server expected");
			var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
			if (!int.TryParse(sServerPort, out var serverPort))
				throw new ArgumentException("Invalid mqttClient connectionString: Port expected");

			var exceptionsTopicPattern = tokens.GetOrDefault(ExceptionTopicKey) ?? DefaultExceptionTopicPattern;
			var deviceStatusEventsTopicPattern = tokens.GetOrDefault(DeviceStatusEventsTopicKey) ?? DefaultDeviceStatusEventsTopicPattern;
			var tagScheduleEventsTopicPattern = tokens.GetOrDefault(TagReadEventsTopicKey) ?? DefaultTagReadEventsTopicPattern;
			var tagWriteRequestCommandsTopicRoot = tokens.GetOrDefault(TagWriteRequestCommandsTopicRootKey) ?? DefaultTagWriteRequestCommandsTopicRoot;
			var username = tokens.GetOrDefault(UsernameKey) ?? string.Empty;
			var password = tokens.GetOrDefault(PasswordKey) ?? string.Empty;

			return new MqttClientEventPublisherOptions(clientId, serverAddress, serverPort, exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopicRoot, username, password);
		}

		private MqttServerEventPublisherOptions ParseMqttServerEventPublisherOptions(IDictionary<string, string> tokens)
		{
			var serverId = tokens.GetOrDefault(ServerIdKey) ?? Guid.NewGuid().ToString("N");
			var sServerPort = tokens.GetOrDefault(PortKey) ?? string.Empty;
			if (!int.TryParse(sServerPort, out var serverPort))
				throw new ArgumentException("Invalid mqttServer connectionString: port expected");

			var exceptionsTopicPattern = tokens.GetOrDefault(ExceptionTopicKey) ?? DefaultExceptionTopicPattern;
			var deviceStatusEventsTopicPattern = tokens.GetOrDefault(DeviceStatusEventsTopicKey) ?? DefaultDeviceStatusEventsTopicPattern;
			var tagScheduleEventsTopicPattern = tokens.GetOrDefault(TagReadEventsTopicKey) ?? DefaultTagReadEventsTopicPattern;
			var tagWriteRequestCommandsTopic = tokens.GetOrDefault(TagWriteRequestCommandsTopicRootKey) ?? DefaultTagWriteRequestCommandsTopicRoot;
			var isPublishPartialReads = tokens.GetOrDefault(PublishPartialReadsKey) != "false";

			return new MqttServerEventPublisherOptions(serverId, serverPort, exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopic, isPublishPartialReads);
		}
	}
}
