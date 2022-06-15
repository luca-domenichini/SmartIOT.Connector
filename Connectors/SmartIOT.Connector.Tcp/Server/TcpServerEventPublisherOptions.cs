namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerEventPublisherOptions
	{
		public int ServerPort { get; }
		public bool IsPublishPartialReads { get; set; }

		public TcpServerEventPublisherOptions(int serverPort, bool isPublishPartialReads)
		{
			ServerPort = serverPort;
			IsPublishPartialReads = isPublishPartialReads;
		}
	}
}
