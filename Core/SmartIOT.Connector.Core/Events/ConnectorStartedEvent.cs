namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorStartedEvent
	{
		public IConnector Connector { get; }
		public string Info { get; }

		public ConnectorStartedEvent(IConnector connector, string info)
		{
			Connector = connector;
			Info = info;
		}
	}
}
