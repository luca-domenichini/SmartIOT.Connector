using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
	public interface IConnectorEventPublisher
	{
		void Start(IConnector connector, ISmartIOTConnectorInterface connectorInterface);
		void Stop();

		void PublishTagScheduleEvent(TagScheduleEvent e);
		void PublishDeviceStatusEvent(DeviceStatusEvent e);
		void PublishException(Exception exception);
	}
}
