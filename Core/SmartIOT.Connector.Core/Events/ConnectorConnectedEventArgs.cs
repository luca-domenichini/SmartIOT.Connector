namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorConnectedEventArgs : EventArgs
	{
		public ConnectorConnectedEvent Event { get; }

		public ConnectorConnectedEventArgs(ConnectorConnectedEvent @event)
		{
			Event = @event;
		}
	}
}
