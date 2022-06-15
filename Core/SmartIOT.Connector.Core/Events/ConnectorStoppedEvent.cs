namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorStoppedEvent
	{
		public IConnector Connector { get; }
		public string Info { get; }

		public ConnectorStoppedEvent(IConnector connector, string info)
		{
			Connector = connector;
			Info = info;
		}
	}
}
