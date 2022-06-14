using SmartIOT.Connector.Core.Connector;

namespace SmartIOT.Connector.Tcp
{
	public class TcpConnector : AbstractPublisherConnector
	{
		public TcpConnector(ConnectorOptions options, IConnectorEventPublisher eventPublisher)
			: base(options, eventPublisher)
		{
		}
	}
}
