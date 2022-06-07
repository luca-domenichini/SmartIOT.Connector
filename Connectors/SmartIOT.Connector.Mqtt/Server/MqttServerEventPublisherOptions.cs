using SmartIOT.Connector.Mqtt;

namespace SmartIOT.Connector.Mqtt.Server
{
	public class MqttServerEventPublisherOptions : MqttEventPublisherOptions
	{
		public string ServerId { get; }
		public int ServerPort { get; }
		public bool IsPublishPartialReads { get; set; }

		public MqttServerEventPublisherOptions(string serverId, int serverPort, string exceptionsTopicPattern, string deviceStatusEventsTopicPattern, string tagScheduleEventsTopicPattern, string tagWriteRequestCommandsTopicRoot, bool isPublishPartialReads)
			: base(exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopicRoot)
		{
			ServerId = serverId;
			ServerPort = serverPort;
			IsPublishPartialReads = isPublishPartialReads;
		}
	}
}
