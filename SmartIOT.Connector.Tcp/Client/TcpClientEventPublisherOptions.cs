namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientEventPublisherOptions
	{
		public string ServerAddress { get; }
		public int ServerPort { get; }
		public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

		public TcpClientEventPublisherOptions(string serverAddress, int serverPort)
		{
			ServerAddress = serverAddress;
			ServerPort = serverPort;
		}
	}
}
