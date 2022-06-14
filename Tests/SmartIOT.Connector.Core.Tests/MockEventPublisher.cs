using Moq;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using System;

namespace SmartIOT.Connector.Core.Tests
{
	public class MockEventPublisher : Mock<IConnectorEventPublisher>, IConnectorEventPublisher
	{
		private ConnectorInterface? _connectorInterface;

		public void Start(IConnector connector, ConnectorInterface connectorInterface)
		{
			_connectorInterface = connectorInterface;
			Object.Start(connector, connectorInterface);
		}

		public void Stop()
		{
			Object.Stop();
		}

		public void PublishException(Exception exception)
		{
			Object.PublishException(exception);
		}

		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			Object.PublishDeviceStatusEvent(e);
		}

		public void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			Object.PublishTagScheduleEvent(e);
		}

		public void RequestTagWrite(TagWriteRequestCommand command)
		{
			_connectorInterface?.RequestTagWriteDelegate.Invoke(command.DeviceId, command.TagId, command.StartOffset, command.Data);
		}
	}
}
