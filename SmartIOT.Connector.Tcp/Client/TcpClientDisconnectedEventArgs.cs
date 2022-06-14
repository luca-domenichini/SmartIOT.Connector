namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientDisconnectedEventArgs : EventArgs
	{
		public string ServerAddress { get; }
		public int ServerPort { get; }
		public Exception Exception { get; }

		public TcpClientDisconnectedEventArgs(string serverAddress, int serverPort, Exception exception)
		{
			ServerAddress = serverAddress;
			ServerPort = serverPort;
			Exception = exception;
		}
	}
}
