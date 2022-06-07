using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Mqtt
{
	public interface IMqttEventPublisher
	{
		void Start(IConnector schedulerConnector, ConnectorInterface connectorInterface);
		void Stop();

		void PublishTagScheduleEvent(TagScheduleEvent e);
		void PublishDeviceStatusEvent(DeviceStatusEvent e);
		void PublishException(Exception exception);
	}
}
