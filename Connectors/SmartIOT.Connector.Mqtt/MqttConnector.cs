using SmartIOT.Connector.Core.Connector;

namespace SmartIOT.Connector.Mqtt
{
	public class MqttConnector : AbstractPublisherConnector
	{
		public MqttConnector(ConnectorOptions options, IConnectorEventPublisher eventPublisher)
			: base(options, eventPublisher)
		{
		}
	}
}
