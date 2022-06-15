using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerClientDisconnectedEventArgs : EventArgs
	{
		public TcpClient TcpClient { get; }
		public Exception Exception { get; }

		public TcpServerClientDisconnectedEventArgs(TcpClient tcpClient, Exception exception)
		{
			TcpClient = tcpClient;
			Exception = exception;
		}
	}
}
