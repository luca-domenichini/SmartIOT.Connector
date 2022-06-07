namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorDisconnectedEventArgs : EventArgs
	{
		public ConnectorDisconnectedEvent Event { get; }

		public ConnectorDisconnectedEventArgs(ConnectorDisconnectedEvent @event)
		{
			Event = @event;
		}
	}
}
