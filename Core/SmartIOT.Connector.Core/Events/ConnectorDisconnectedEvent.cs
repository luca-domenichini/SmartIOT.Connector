namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorDisconnectedEvent
	{
		public IConnector Connector { get; }
		public string Info { get; }

		public ConnectorDisconnectedEvent(IConnector connector, string info)
		{
			Connector = connector;
			Info = info;
		}
	}
}
