namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorConnectionFailedEventArgs : EventArgs
	{
		public ConnectorConnectionFailedEvent Event { get; }

		public ConnectorConnectionFailedEventArgs(ConnectorConnectionFailedEvent @event)
		{
			Event = @event;
		}
	}
}
