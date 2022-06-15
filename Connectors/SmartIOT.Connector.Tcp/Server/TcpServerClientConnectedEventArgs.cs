using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerClientConnectedEventArgs : EventArgs
	{
		public TcpClient TcpClient { get; }

		public TcpServerClientConnectedEventArgs(TcpClient tcpClient)
		{
			TcpClient = tcpClient;
		}
	}
}
