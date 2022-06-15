namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientConnectedEventArgs : EventArgs
	{
		public string ServerAddress { get; }
		public int ServerPort { get; }

		public TcpClientConnectedEventArgs(string serverAddress, int serverPort)
		{
			ServerAddress = serverAddress;
			ServerPort = serverPort;
		}
	}
}
