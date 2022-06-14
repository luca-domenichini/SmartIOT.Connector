using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Tcp
{
	public interface ITcpEventPublisher
	{
		void Start(TcpConnector connector, ConnectorInterface connectorInterface);
		void Stop();

		void PublishTagScheduleEvent(TagScheduleEvent e);
		void PublishDeviceStatusEvent(DeviceStatusEvent e);
		void PublishException(Exception exception);
	}
}
