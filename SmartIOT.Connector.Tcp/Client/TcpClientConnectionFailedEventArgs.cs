namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientConnectionFailedEventArgs : EventArgs
	{
		public string ServerAddress { get; }
		public int ServerPort { get; }
		public Exception Exception { get; }

		public TcpClientConnectionFailedEventArgs(string serverAddress, int serverPort, Exception exception)
		{
			ServerAddress = serverAddress;
			ServerPort = serverPort;
			Exception = exception;
		}
	}
}
