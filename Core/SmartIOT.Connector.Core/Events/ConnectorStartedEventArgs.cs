namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorStartedEventArgs : EventArgs
	{
		public ConnectorStartedEvent Event { get; }

		public ConnectorStartedEventArgs(ConnectorStartedEvent @event)
		{
			Event = @event;
		}
	}
}
