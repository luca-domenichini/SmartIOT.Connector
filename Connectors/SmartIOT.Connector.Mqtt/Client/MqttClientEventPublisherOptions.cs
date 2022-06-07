using SmartIOT.Connector.Mqtt;

namespace SmartIOT.Connector.Mqtt.Client
{
	public class MqttClientEventPublisherOptions : MqttEventPublisherOptions
	{
		public string ClientId { get; }
		public string ServerAddress { get; }
		public int ServerPort { get; }
		public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
		public string Username { get; set; }
		public string Password { get; set; }

		public MqttClientEventPublisherOptions(string clientId, string serverAddress, int serverPort, string exceptionsTopicPattern, string deviceStatusEventsTopicPattern, string tagScheduleEventsTopicPattern, string tagWriteRequestCommandsTopicRoot, string username, string password)
			: base(exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopicRoot)
		{
			ClientId = clientId;
			ServerAddress = serverAddress;
			ServerPort = serverPort;
			Username = username;
			Password = password;
		}
	}
}
