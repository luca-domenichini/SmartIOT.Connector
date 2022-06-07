namespace SmartIOT.Connector.Mqtt
{
	public class MqttEventPublisherOptions
	{
		public string ExceptionsTopicPattern { get; }
		public string DeviceStatusEventsTopicPattern { get; }
		public string TagScheduleEventsTopicPattern { get; }
		public string TagWriteRequestCommandsTopicRoot { get; }

		public MqttEventPublisherOptions(string exceptionsTopicPattern, string deviceStatusEventsTopicPattern, string tagScheduleEventsTopicPattern, string tagWriteRequestCommandsTopicRoot)
		{
			ExceptionsTopicPattern = exceptionsTopicPattern;
			DeviceStatusEventsTopicPattern = deviceStatusEventsTopicPattern;
			TagScheduleEventsTopicPattern = tagScheduleEventsTopicPattern;
			TagWriteRequestCommandsTopicRoot = tagWriteRequestCommandsTopicRoot;
		}

		public bool IsDeviceStatusEventsTopicRoot(string topic)
		{
			if (DeviceStatusEventsTopicPattern.Contains('/'))
			{
				var root = DeviceStatusEventsTopicPattern[..(DeviceStatusEventsTopicPattern.IndexOf('/') + 1)];
				return topic.StartsWith(root);
			}
			else
			{
				return topic.StartsWith(DeviceStatusEventsTopicPattern);
			}
		}
		public string GetDeviceStatusEventsTopic(string deviceId)
		{
			return DeviceStatusEventsTopicPattern.Replace("${DeviceId}", deviceId.ToString());
		}
		public bool IsTagScheduleEventsTopicRoot(string topic)
		{
			if (TagScheduleEventsTopicPattern.Contains('/'))
			{
				var root = TagScheduleEventsTopicPattern[..(TagScheduleEventsTopicPattern.IndexOf('/') + 1)];
				return topic.StartsWith(root);
			}
			else
			{
				return topic.StartsWith(TagScheduleEventsTopicPattern);
			}
		}
		public string GetTagScheduleEventsTopic(string deviceId, int tagId)
		{
			return TagScheduleEventsTopicPattern.Replace("${DeviceId}", deviceId.ToString()).Replace("${TagId}", tagId.ToString());
		}

	}
}
