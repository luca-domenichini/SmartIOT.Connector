namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorStoppedEventArgs : EventArgs
	{
		public ConnectorStoppedEvent Event { get; }

		public ConnectorStoppedEventArgs(ConnectorStoppedEvent @event)
		{
			Event = @event;
		}
	}
}
