namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorConnectedEvent
	{
		public IConnector Connector { get; }
		public string Info { get; }

		public ConnectorConnectedEvent(IConnector connector, string info)
		{
			Connector = connector;
			Info = info;
		}
	}
}
