namespace SmartIOT.Connector.Core.Connector
{
	public class ConnectorOptions
	{
		public bool IsPublishWriteEvents { get; }

		public ConnectorOptions(bool isPublishWriteEvents)
		{
			IsPublishWriteEvents = isPublishWriteEvents;
		}
	}
}
