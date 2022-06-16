namespace SmartIOT.Connector.Core.Events
{
	public class ConnectorConnectionFailedEventArgs : EventArgs
	{
		public IConnector Connector { get; }
		public string Info { get; }
		public Exception Exception { get; }

		public ConnectorConnectionFailedEventArgs(IConnector connector, string info, Exception exception)
		{
			Connector = connector;
			Info = info;
			Exception = exception;
		}
	}
}
