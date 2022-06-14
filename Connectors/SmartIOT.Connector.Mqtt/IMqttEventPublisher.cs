﻿using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Mqtt
{
	public interface IMqttEventPublisher
	{
		void Start(MqttConnector connector, ConnectorInterface connectorInterface);
		void Stop();

		void PublishTagScheduleEvent(TagScheduleEvent e);
		void PublishDeviceStatusEvent(DeviceStatusEvent e);
		void PublishException(Exception exception);
	}
}
