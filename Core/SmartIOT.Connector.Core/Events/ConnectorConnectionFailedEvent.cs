namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorConnectionFailedEvent
	{
		public IConnector Connector { get; }
		public Exception Exception { get; }

		public ConnectorConnectionFailedEvent(IConnector connector, Exception exception)
		{
			Connector = connector;
			Exception = exception;
		}
	}
}
