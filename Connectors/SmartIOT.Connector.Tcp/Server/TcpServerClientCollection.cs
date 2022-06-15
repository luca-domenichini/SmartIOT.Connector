using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Server
{
	internal class TcpServerClientCollection : IEnumerable<TcpClient>
	{
		private IDictionary<TcpClient, byte> _clients = new ConcurrentDictionary<TcpClient, byte>();

		public void Add(TcpClient tcpClient)
		{
			_clients[tcpClient] = 1;
		}
		public bool Remove(TcpClient tcpClient)
		{
			return _clients.Remove(tcpClient);
		}

		public IEnumerator<TcpClient> GetEnumerator()
		{
			return _clients.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _clients.Keys.GetEnumerator();
		}
	}
}
