using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Mqtt;

namespace SmartIOT.Connector.Mqtt.Client
{
	public class MqttClientConnectorOptions : MqttConnectorOptions
	{
		public string ClientId { get; }
		public string ServerAddress { get; }
		public int ServerPort { get; }
		public TimeSpan ReconnectDelay { get; }
		public string Username { get; set; }
		public string Password { get; set; }

		public MqttClientConnectorOptions(bool isPublishWriteEvents, ISingleMessageSerializer messageSerializer, string clientId, string serverAddress, int serverPort, string exceptionsTopicPattern, string deviceStatusEventsTopicPattern, string tagScheduleEventsTopicPattern, string tagWriteRequestCommandsTopicRoot, TimeSpan reconnectDelay, string username, string password)
			: base(isPublishWriteEvents, messageSerializer, exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopicRoot)
		{
			ClientId = clientId;
			ServerAddress = serverAddress;
			ServerPort = serverPort;
			ReconnectDelay = reconnectDelay;
			Username = username;
			Password = password;
		}
	}
}
